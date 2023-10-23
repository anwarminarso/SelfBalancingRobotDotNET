using SelfBalancingRobot.WebUI.Hubs;

namespace SelfBalancingRobot.WebUI.Models;

public class ControlContext
{
    private readonly IMUContext imuContext;
    
    public bool ArmDisarm { get; private set; }
    public float ControlX { get; private set; }
    public float ControlY { get; private set; }

    public ControlContext(IMUContext imuContext)
    {
        this.imuContext = imuContext;
    }
    public void Init()
    {
    }

    public void RCCommand(float joyX, float joyY)
    {
        ControlX = joyX;
        ControlY = joyY;
    }
}
