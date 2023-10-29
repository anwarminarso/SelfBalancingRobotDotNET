using Iot.Device.Gpio.Drivers;
using SelfBalancingRobot.WebUI.Configuration;
using System.Device.Gpio;

namespace SelfBalancingRobot.WebUI.Models;


/// <summary>
/// Development Purpose
/// </summary>
public class MotorContext
{
    private readonly DeviceSettings deviceSettings;
    private bool isStandby = true;

    public bool IsStandby
    {
        get { return isStandby; }
        set
        {
            if (isStandby == value)
                return;
            if (value)
                Standby();
            else
                Activate();
        }
    }

    public MotorContext(DeviceSettings deviceSettings)
    {
        this.deviceSettings = deviceSettings;
    }

    public void Init()
    {
        isStandby = true;
    }
    public void Standby()
    {
        isStandby = true;
    }
    public void Activate()
    {
        isStandby = false;
    }
    public void Drive(double leftSpeed, double rightSpeed)
    {
        if (isStandby)
            return;
    }
    public void Brake()
    {
    }
}

