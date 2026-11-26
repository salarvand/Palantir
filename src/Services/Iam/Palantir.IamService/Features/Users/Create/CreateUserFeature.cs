using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.IamService.Domain;
using Palantir.IamService.Infrastructure;
using Palantir.IamService.IntegrationEvents.Events;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Security;
using Palantir.Platform.Validation;
using Wolverine;

namespace Palantir.IamService.Features.Users.Create;

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role,
    int KycTier,
    decimal MaxLeverage,
    decimal DailyLossLimit);

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).NotEmpty();
    }
}

internal sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/iam/users", async (
                CreateUserRequest request,
                IKeycloakAdminClient keycloakClient,
                IamDbContext db,
                IPermissionTokenManager tokenManager,
                IMessageBus messageBus,
                CancellationToken ct) =>
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

                return TypedResults.Created($"/api/v1/iam/users/{profile.Id}", new UserDto(
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
            })
            .ProducesPost<UserDto>()
            .WithValidation<CreateUserRequest>()
            .WithTags("IAM")
            .WithName(nameof(CreateUserEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowManageUsers);
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
