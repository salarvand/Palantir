using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace Palantir.Gateway;

public sealed class AspireServiceDiscoveryDestinationResolver : IDestinationResolver
{
    private readonly IConfiguration _configuration;

    public AspireServiceDiscoveryDestinationResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(
        IReadOnlyDictionary<string, DestinationConfig> destinations,
        CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<string, DestinationConfig>(destinations.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, dest) in destinations)
        {
            if (Uri.TryCreate(dest.Address, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;
                var resolvedUrl = _configuration[$"services:{host}:http:0"]
                                  ?? _configuration[$"services:{host}:https:0"]
                                  ?? _configuration[$"services:{host}:default:0"];

                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    resolved[key] = new DestinationConfig
                    {
                        Address = resolvedUrl,
                        Health = dest.Health,
                        Metadata = dest.Metadata,
                        Host = dest.Host
                    };
                    continue;
                }
            }

            resolved[key] = dest;
        }

        return ValueTask.FromResult(new ResolvedDestinationCollection(resolved, NullChangeToken.Instance));
    }

    private sealed class NullChangeToken : IChangeToken
    {
        public static NullChangeToken Instance { get; } = new();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
