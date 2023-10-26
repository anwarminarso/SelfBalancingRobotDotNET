using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Configuration;

public class CalibrationSettings
{
    public PIDSetting YawPID { get; set; } = new PIDSetting();
    public PIDSetting PitchPID { get; set; } = new PIDSetting();
    public PIDSetting AnglePID { get; set; } = new PIDSetting();

    public OffsetSettings GyroOffsets { get; set; } = new OffsetSettings();
    public OffsetSettings AccelOffsets { get; set; } = new OffsetSettings();

}

public class PIDSetting
{
    public float P { get; set; }
    public float I { get; set; }
    public float D { get; set; }
}
public class OffsetSettings
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public OffsetSettings()
    {

    }
    public OffsetSettings(Vector3 value)
    {
        this.X = value.X;
        this.Y = value.Y;
        this.Z = value.Z;
    }
    public OffsetSettings(float[] values)
    {
        this.X = values[0];
        this.Y = values[1];
        this.Z = values[2];
    }
    public OffsetSettings(float X, float Y, float Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }
    public Vector3 ToVector3()
    {
        return new Vector3(X, Y, Z);
    }
}
