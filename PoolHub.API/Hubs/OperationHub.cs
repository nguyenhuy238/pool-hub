using Microsoft.AspNetCore.SignalR;

namespace PoolHub.API.Hubs;

public class OperationHub : Hub<IOperationHubClient>
{
}
