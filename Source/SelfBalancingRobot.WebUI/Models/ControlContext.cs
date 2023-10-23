using SelfBalancingRobot.WebUI.Hubs;

namespace SelfBalancingRobot.WebUI.Models;

public class ControlContext
{
    private readonly ControlHub controlHub;
    public ControlContext(ControlHub controlHub)
    {
        this.controlHub = controlHub;
    }
}
