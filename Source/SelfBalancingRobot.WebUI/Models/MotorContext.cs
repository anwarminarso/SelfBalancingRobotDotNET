using Iot.Device.Gpio.Drivers;
using SelfBalancingRobot.WebUI.Configuration;
using System.Device.Gpio;

namespace SelfBalancingRobot.WebUI.Models;

public class MotorContext
{
    private readonly DeviceSettings deviceSettings;
    private MotorDriver leftMotor;
    private MotorDriver rightMotor;

    GpioController gpio;
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

    public MotorContext(GpioController gpio, DeviceSettings deviceSettings)
    {
        this.gpio = gpio;
        this.deviceSettings = deviceSettings;
    }

    public void Init()
    {
        if (!gpio.IsPinModeSupported(deviceSettings.StandbyPin, PinMode.Output))
            throw new Exception($"GPIO Standby Pin {deviceSettings.StandbyPin} is not supported");

        if (gpio.IsPinOpen(deviceSettings.StandbyPin))
            gpio.ClosePin(deviceSettings.StandbyPin);
        gpio.OpenPin(deviceSettings.StandbyPin, PinMode.Output);
        gpio.Write(deviceSettings.StandbyPin, PinValue.Low);
        isStandby = true;

        leftMotor = new MotorDriver(gpio,
            deviceSettings.LeftMotor.PWMChip, deviceSettings.LeftMotor.PWMChannel, deviceSettings.LeftMotor.PWMFrequency,
            deviceSettings.LeftMotor.IN1Pin, deviceSettings.LeftMotor.IN2Pin, deviceSettings.LeftMotor.IsReverse ? -1 : 1);
        rightMotor = new MotorDriver(gpio,
            deviceSettings.RightMotor.PWMChip, deviceSettings.RightMotor.PWMChannel, deviceSettings.RightMotor.PWMFrequency,
            deviceSettings.RightMotor.IN1Pin, deviceSettings.RightMotor.IN2Pin, deviceSettings.RightMotor.IsReverse ? -1 : 1);

        this.leftMotor.Init();
        this.rightMotor.Init();
        leftMotor.SetMotorState(MotorDriver.MotorState.DEFAULT);
        rightMotor.SetMotorState(MotorDriver.MotorState.DEFAULT);
    }
    public void Standby()
    {
        if (!isStandby || gpio.Read(deviceSettings.StandbyPin) == PinValue.High)
        {
            leftMotor.Drive(0);
            rightMotor.Drive(0);
            gpio.Write(deviceSettings.StandbyPin, PinValue.Low);
            isStandby = true;
            leftMotor.SetMotorState(MotorDriver.MotorState.DEFAULT);
            rightMotor.SetMotorState(MotorDriver.MotorState.DEFAULT);
        }
    }
    public void Activate()
    {
        if (isStandby || gpio.Read(deviceSettings.StandbyPin) == PinValue.Low)
        {
            gpio.Write(deviceSettings.StandbyPin, PinValue.High);
            isStandby = false;
        }
    }
    public void Drive(double leftSpeed, double rightSpeed)
    {
        if (isStandby)
            return;
        leftMotor.Drive(leftSpeed);
        rightMotor.Drive(rightSpeed);
    }
    public void Brake()
    {
        leftMotor.Brake(1000);
        rightMotor.Brake(1000);
    }
}

