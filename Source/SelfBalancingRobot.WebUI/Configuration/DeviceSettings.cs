using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SelfBalancingRobot.WebUI.Models;

namespace SelfBalancingRobot.WebUI.Configuration;

public class DeviceSettings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public IMUType IMUType { get; set; }

    public int? IMUAddress { get; set; }
    public int I2CBusId { get; set; }

    public int StandbyPin { get; set; }

    public MotorSettings LeftMotor { get; set; }
    public MotorSettings RightMotor { get; set; }
}

public class MotorSettings
{
    public int PWMChip { get; set; }
    public int PWMChannel { get; set; }
    public int PWMFrequency { get; set; }
    public int IN1Pin { get; set; }
    public int IN2Pin { get; set; }
    public bool IsReverse { get; set; } = false;
    public int? EncoderPin { get; set; } = null;
}
