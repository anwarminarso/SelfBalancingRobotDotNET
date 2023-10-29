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

    [HubMethodName("JoystickEnable")]
    public void JoystickEnable(bool value)
    {
        controlContext.JoystickEnabled = value;
    }

    [HubMethodName("RCCommand")]
    public void RCCommand(float joyX, float joyY)
    {
        controlContext.RCCommand(joyX, joyY);
    }

    [HubMethodName("ArmDisarm")]
    public void ArmDisarm(bool arm)
    {
        if (arm)
            controlContext.Activate();
        else
            controlContext.Deactivate();
    }
}