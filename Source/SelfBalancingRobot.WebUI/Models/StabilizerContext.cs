namespace SelfBalancingRobot.WebUI.Models;

public class StabilizerContext
{
    private readonly IMUContext imuContext;
    private readonly ControlContext controlContext;

    public StabilizerContext(IMUContext imuContext, ControlContext controlContext)
    {
        this.imuContext = imuContext;
        this.controlContext = controlContext;
    }
}
