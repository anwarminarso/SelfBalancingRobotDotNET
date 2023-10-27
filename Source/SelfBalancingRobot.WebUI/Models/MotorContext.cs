namespace SelfBalancingRobot.WebUI.Models;

public class MotorContext
{
    private MotorDriver leftMotor;
    private MotorDriver rightMotor;
    public MotorContext(MotorDriver leftMotor, MotorDriver rightMotor)
    {
        this.leftMotor = leftMotor;
        this.rightMotor = rightMotor;
    }

    public void Init()
    {
        this.leftMotor.Init();
        this.rightMotor.Init();
    }
    public void Active()
    {
        leftMotor.Activate();
        rightMotor.Activate();
    }
    public void Standby()
    {
        leftMotor.Standby();
        rightMotor.Standby();
    }
    public void Drive(double leftSpeed, double rightSpeed)
    {
        leftMotor.Drive(leftSpeed);
        rightMotor.Drive(rightSpeed);
    }
    public void Brake()
    {
        leftMotor.Brake(1000);
        rightMotor.Brake(1000);
    }
}
