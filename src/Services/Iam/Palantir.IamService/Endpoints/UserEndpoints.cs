using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Iam.Contracts;
using Palantir.IamService.Domain;
using Palantir.IamService.Infrastructure;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Security;
using Wolverine;

namespace Palantir.IamService.Endpoints;

public class UserEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/iam")
            .WithTags("IAM");

        group.MapPost("/users", CreateUserAsync);
        group.MapGet("/users", GetUsersAsync);
        group.MapGet("/users/{id:guid}", GetUserByIdAsync);
        group.MapPut("/users/{id:guid}/permissions", AssignPermissionsAsync);
        group.MapPost("/token/exchange", ExchangeTokenAsync);
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        IKeycloakAdminClient keycloakClient,
        IamDbContext db,
        IPermissionTokenManager tokenManager,
        IMessageBus messageBus,
        CancellationToken ct)
    {
        var keycloakUserId = await keycloakClient.CreateUserInKeycloakAsync(
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            request.Role,
            ct);

        var defaultPermissions = GetDefaultPermissionsForRole(request.Role);

        var profile = new TradingProfile(
            keycloakUserId,
            request.Username,
            request.Email,
            request.Role,
            request.KycTier,
            request.MaxLeverage,
            request.DailyLossLimit)
        {
            AssignedPermissions = defaultPermissions
        };

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(ct);

        var token = new PermissionToken(
            profile.Id,
            profile.Username,
            [profile.Role],
            defaultPermissions,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(30));

        await tokenManager.SetPermissionTokenAsync(token, TimeSpan.FromMinutes(30), ct);

        await messageBus.PublishAsync(new UserCreatedIntegrationEvent(
            profile.Id,
            profile.Username,
            profile.Email,
            profile.Role,
            DateTimeOffset.UtcNow));

        return Results.Created($"/api/v1/iam/users/{profile.Id}", new UserDto(
            profile.Id,
            profile.Username,
            profile.Email,
            request.FirstName,
            request.LastName,
            true,
            profile.Role,
            profile.KycTier,
            profile.MaxLeverage,
            profile.DailyLossLimit,
            defaultPermissions));
    }

    private static async Task<IResult> GetUsersAsync(IamDbContext db, CancellationToken ct)
    {
        var profiles = await db.Profiles.AsNoTracking().ToListAsync(ct);
        var dtos = profiles.Select(p => new UserDto(
            p.Id,
            p.Username,
            p.Email,
            string.Empty,
            string.Empty,
            true,
            p.Role,
            p.KycTier,
            p.MaxLeverage,
            p.DailyLossLimit,
            p.AssignedPermissions));

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetUserByIdAsync(Guid id, IamDbContext db, CancellationToken ct)
    {
        var profile = await db.Profiles.FindAsync([id], ct);
        if (profile == null) return Results.NotFound();

        return Results.Ok(new UserDto(
            profile.Id,
            profile.Username,
            profile.Email,
            string.Empty,
            string.Empty,
            true,
            profile.Role,
            profile.KycTier,
            profile.MaxLeverage,
            profile.DailyLossLimit,
            profile.AssignedPermissions));
    }

    private static async Task<IResult> AssignPermissionsAsync(
        Guid id,
        AssignPermissionsRequest request,
        IamDbContext db,
        IPermissionTokenManager tokenManager,
        IMessageBus messageBus,
        CancellationToken ct)
    {
        var profile = await db.Profiles.FindAsync([id], ct);
        if (profile == null) return Results.NotFound();

        profile.AssignedPermissions = request.Permissions.ToList();
        await db.SaveChangesAsync(ct);

        var token = new PermissionToken(
            profile.Id,
            profile.Username,
            [profile.Role],
            profile.AssignedPermissions,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(30));

        await tokenManager.SetPermissionTokenAsync(token, TimeSpan.FromMinutes(30), ct);

        await messageBus.PublishAsync(new PermissionsUpdatedIntegrationEvent(
            profile.Id,
            profile.AssignedPermissions,
            DateTimeOffset.UtcNow));

        return Results.Ok(profile.AssignedPermissions);
    }

    private static async Task<IResult> ExchangeTokenAsync(
        Guid userId,
        IamDbContext db,
        IPermissionTokenManager tokenManager,
        CancellationToken ct)
    {
        var cached = await tokenManager.GetPermissionTokenAsync(userId, ct);
        if (cached != null) return Results.Ok(cached);

        var profile = await db.Profiles.FindAsync([userId], ct);
        if (profile == null) return Results.NotFound("User profile not found.");

        var token = new PermissionToken(
            profile.Id,
            profile.Username,
            [profile.Role],
            profile.AssignedPermissions,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(30));

        await tokenManager.SetPermissionTokenAsync(token, TimeSpan.FromMinutes(30), ct);

        return Results.Ok(token);
    }

    private static List<string> GetDefaultPermissionsForRole(string role) =>
        role.ToLowerInvariant() switch
        {
            "admin" => Permissions.All.ToList(),
            "trader" => [Permissions.AllowPlaceOrder, Permissions.AllowCancelOrder, Permissions.AllowViewBlotter, Permissions.AllowViewPortfolio],
            "riskofficer" => [Permissions.AllowOverrideRisk, Permissions.AllowViewRiskExposure, Permissions.AllowManageRiskLimits, Permissions.AllowHaltTrading],
            "auditor" => [Permissions.AllowViewAuditLogs, Permissions.AllowExportAuditLogs],
            _ => [Permissions.AllowViewBlotter]
        };
}
