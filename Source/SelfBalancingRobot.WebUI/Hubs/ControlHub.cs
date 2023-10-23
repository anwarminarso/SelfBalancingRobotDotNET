using Microsoft.AspNetCore.SignalR;
using SelfBalancingRobot.WebUI.Models;

namespace SelfBalancingRobot.WebUI.Hubs;

public class ControlHub : Hub
{
    private readonly ControlContext controlContext;

    public ControlHub(ControlContext controlContext)
    {
        this.controlContext = controlContext;
    }
    [HubMethodName("RCCommand")]
    public async Task RCCommand(float joyX, float joyY)
    {
        controlContext.RCCommand(joyX, joyY);
        await Task.Delay(0);
    }

    [HubMethodName("ArmDisarm")]
    public async Task ArmDisarm(bool arm)
    {
        await Task.Delay(0);
    }
}