using Microsoft.AspNetCore.SignalR;

namespace Palantir.MarketDataService.Hubs;

public class MarketDataHub : Hub
{
    public async Task SubscribeSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());
    }

    public async Task UnsubscribeSymbol(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());
    }
}
