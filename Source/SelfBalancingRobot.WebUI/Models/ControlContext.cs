using Iot.Device.Mpu6886;
using SelfBalancingRobot.WebUI.Hubs;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Models;

public class ControlContext : BaseMonitoringContext
{
    private readonly IMUContext imuContext;
    private readonly MotorContext motorContext;
    private readonly StabilizerContext stabilizerContext;

    private bool armed = false;
    public bool Armed
    {
        get { return armed; }
    }
    private bool joyEnabled = false;
    public bool JoystickEnabled { get; set; }

    public float ControlX { get; set; }
    public float ControlY { get; set; }

    private const int StabilizerFrequency = 50;
    private int loopDelay = 0;


    public ControlContext(IMUContext imuContext, MotorContext motorContext, StabilizerContext stabilizerContext)
    {
        this.stabilizerContext = stabilizerContext;
        this.imuContext = imuContext;
        this.motorContext = motorContext;
    }
    public void Activate()
    {
        if (armed)
            return;
        ControlX = 0;
        ControlY = 0;
        motorContext.Activate();
        //StartMonitoring();
        imuContext.OnIMUUpdated += OnIMUUpdated;
        armed = true;
    }
    public void Deactivate()
    {
        if (!armed)
            return;
        StopMonitoring();
        imuContext.OnIMUUpdated -= OnIMUUpdated;
        motorContext.Standby();
        armed = false;
    }

    private void OnIMUUpdated(Vector3 ypr, Vector3 gyro, Vector3 acc)
    {
        (float leftSpeed, float rightSpeed) = stabilizerContext.Stabilize(gyro, ypr, ControlX, ControlY);
        motorContext.Drive(leftSpeed, rightSpeed);
    }

    public void RCCommand(float joyX, float joyY)
    {
        ControlX = joyX;
        ControlY = joyY;
        stabilizerContext.RCOnline = true;
    }

    public override void OnStartMonitoring()
    {
        loopDelay = Convert.ToInt32(1000 / StabilizerFrequency);
        stabilizerContext.Reset();
    }
    public override void MonitoringLoop()
    {
        base.monitoringTaskToken.Token.WaitHandle.WaitOne(loopDelay);
        var ypr = imuContext.GetYPR();
        var gyro = imuContext.GetGyro();
        (float leftSpeed, float rightSpeed) = stabilizerContext.Stabilize(gyro, ypr, ControlX, ControlY);
        motorContext.Drive(leftSpeed, rightSpeed);
    }
}
