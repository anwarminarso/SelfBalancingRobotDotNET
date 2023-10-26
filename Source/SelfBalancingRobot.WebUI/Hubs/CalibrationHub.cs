using Microsoft.AspNetCore.SignalR;
using SelfBalancingRobot.WebUI.Models;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Hubs;

public class CalibrationHub : Hub
{
    [HubMethodName("SubscribeGyro")]
    public void SubscribeGyro()
    {
        this.Groups?.AddToGroupAsync(Context.ConnectionId, "Gyro")?.ConfigureAwait(false);
    }
    [HubMethodName("UnsubscribeGyro")]
    public void UnsubscribeGyro()
    {
        this.Groups?.RemoveFromGroupAsync(Context.ConnectionId, "Gyro")?.ConfigureAwait(false);
    }
    [HubMethodName("SubscribeAcc")]
    public void SubscribeAcc()
    {
        this.Groups?.AddToGroupAsync(Context.ConnectionId, "Acc")?.ConfigureAwait(false);
    }
    [HubMethodName("UnsubscribeAcc")]
    public void UnsubscribeAcc()
    {
        this.Groups?.RemoveFromGroupAsync(Context.ConnectionId, "Acc")?.ConfigureAwait(false);
    }
    public void SendRawGyro(Vector3 value)
    {
        this.Clients?.Group("Gyro")?.SendAsync("UpdateGyro", new float[] { value.X, value.Y, value.Z })?.ConfigureAwait(false);
    }
    public void SendRawAcc(Vector3 value)
    {
        this.Clients?.Group("Acc")?.SendAsync("UpdateAcc", new float[] { value.X, value.Y, value.Z })?.ConfigureAwait(false);
    }
}
