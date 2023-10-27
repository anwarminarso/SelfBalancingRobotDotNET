using System.Device.Gpio;
using System.Device.Pwm;
using UnitsNet;

namespace SelfBalancingRobot.WebUI.Models;

public class MotorDriver
{
    private GpioController gpio;
    private PwmChannel pwm;
    private int pwmChip = 0;
    private int pwmChannel = 0;
    private int pwmFreq = 400;
    private int pinIN1 = 260; // PI04, pin 26, physical pin 38
    private int pinIN2 = 259; // PI03 pin 27, physical pin 40
    private int pinPWM = 269; // PWM3 pin 2, physical pin 7
    private int pinStandby = 268;
    private int offset;
    private MotorState motorState = MotorState.DEFAULT;

    private double PWMValue = 0;

    bool isStandby = true;
    public MotorDriver(GpioController gpio, int pwmChip, int pwmChannel, int pwmFreq, int pinPWM, int pinIN1, int pinIN2, int pinStandby, int offset = 1)
    {
        this.gpio = gpio;
        this.pwmChip = pwmChip;
        this.pwmChannel = pwmChannel;
        this.pinPWM = pinPWM;
        this.pinIN1 = pinIN1;
        this.pinIN2 = pinIN2;
        this.pinStandby = pinStandby;
        this.offset = offset;
    }
    public void Init()
    {
        if (!gpio.IsPinModeSupported(pinIN1, PinMode.Output))
            throw new Exception($"GPIO IN1 Pin {pinIN1} is not supported");
        if (!gpio.IsPinModeSupported(pinIN2, PinMode.Output))
            throw new Exception($"GPIO IN2 Pin {pinIN2} is not supported");
        if (!gpio.IsPinModeSupported(pinStandby, PinMode.Output))
            throw new Exception($"GPIO Standby Pin {pinStandby} is not supported");
        if (gpio.IsPinOpen(pinIN1))
            gpio.ClosePin(pinIN1);
        if (gpio.IsPinOpen(pinIN2))
            gpio.ClosePin(pinIN2);
        if (gpio.IsPinOpen(pinStandby))
            gpio.ClosePin(pinStandby);
        gpio.OpenPin(pinIN1, PinMode.Output);
        gpio.OpenPin(pinIN2, PinMode.Output);
        gpio.OpenPin(pinStandby, PinMode.Output);
        pwm = PwmChannel.Create(pwmChip, pwmChannel, pwmFreq, PWMValue);
        pwm.Start();
    }
    public void Activate()
    {
        if (isStandby || gpio.Read(pinStandby) == PinValue.Low)
        {
            gpio.Write(pinStandby, PinValue.High);
            isStandby = false;
        }
    }
    public void Standby()
    {
        if (!isStandby || gpio.Read(pinStandby) == PinValue.High)
        {
            gpio.Write(pinStandby, PinValue.Low);
            isStandby = true;
            SetMotorState(MotorState.DEFAULT);
        }
    }

    /// <summary>
    /// Drive the motor
    /// </summary>
    /// <param name="speed">Speed is between -1000 and 1000</param>
    public void Drive(double speed)
    {
        if (isStandby)
            Activate();
        speed = speed * offset;
        if (speed > 1000)
            speed = 1000;
        if (speed < -1000)
            speed = -1000;
        PWMValue = (speed / 1000.0);
        if (PWMValue >= 0)
        {
            SetMotorState(MotorState.FORWARD);
            pwm.DutyCycle = PWMValue;
        }
        else if (PWMValue < 0)
        {
            SetMotorState(MotorState.REVERSE);
            pwm.DutyCycle = -PWMValue;
        }
    }

    /// <summary>
    /// Brake the motor
    /// </summary>
    /// <param name="power">Brake Power is between -1000 and 1000</param>
    public void Brake(double power)
    {
        power = power * offset;
        if (power > 1000)
            power = 1000;
        if (power < -1000)
            power = -1000;
        PWMValue = Math.Abs(power / 1000.0);
        SetMotorState(MotorState.BRAKE);
        pwm.DutyCycle = PWMValue;
    }

    private void SetMotorState(MotorState value)
    {
        if (motorState == value)
            return;
        switch (value)
        {
            case MotorState.FORWARD:
                gpio.Write(pinIN1, PinValue.High);
                gpio.Write(pinIN2, PinValue.Low);
                break;
            case MotorState.REVERSE:
                gpio.Write(pinIN1, PinValue.Low);
                gpio.Write(pinIN2, PinValue.High);
                break;
            case MotorState.BRAKE:
                gpio.Write(pinIN1, PinValue.High);
                gpio.Write(pinIN2, PinValue.High);
                break;
            case MotorState.DEFAULT:
            default:
                gpio.Write(pinIN1, PinValue.Low);
                gpio.Write(pinIN2, PinValue.Low);
                break;
        }
        motorState = value;
    }
    private enum MotorState
    {
        DEFAULT,
        FORWARD,
        REVERSE,
        BRAKE
    }
}
