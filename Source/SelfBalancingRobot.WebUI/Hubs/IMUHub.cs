using Microsoft.AspNetCore.SignalR;
using SelfBalancingRobot.WebUI.Models;

namespace SelfBalancingRobot.WebUI.Hubs;

public class IMUHub : Hub
{
    public void SendUpdate(DataHistory value)
    {
        this.Clients?.All?.SendAsync("Update", value)?.ConfigureAwait(false);
    }
}
