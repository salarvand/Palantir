using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Palantir.Platform.Grpc;

public static class GrpcExtensions
{
    public static IHttpClientBuilder AddResilientGrpcClient<TClient>(
        this IServiceCollection services,
        Uri baseAddress)
        where TClient : ClientBase<TClient>
    {
        return services.AddGrpcClient<TClient>(options =>
        {
            options.Address = baseAddress;
        })
        .ConfigureChannel(channelOptions =>
        {
            var defaultMethodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 4,
                    InitialBackoff = TimeSpan.FromMilliseconds(50),
                    MaxBackoff = TimeSpan.FromSeconds(1),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                }
            };

            channelOptions.ServiceConfig = new ServiceConfig
            {
                MethodConfigs = { defaultMethodConfig }
            };
        });
    }
}
