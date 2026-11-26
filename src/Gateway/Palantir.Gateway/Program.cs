using Palantir.Gateway;
using Palantir.Platform.Caching;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Yarp.ReverseProxy.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("PalantirDb")
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMicroUI", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:3003",
                "http://localhost:3100",
                "http://localhost:3101")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<IDestinationResolver, AspireServiceDiscoveryDestinationResolver>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors("AllowMicroUI");
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();
