using Palantir.Gateway;
using Palantir.ServiceDefaults;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMicroUI", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "http://localhost:3003")
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

app.MapReverseProxy();

app.Run();
