using Iot.Device.Mcp25xxx.Register;
using Iot.Device.SensorHub;
using SelfBalancingRobot.WebUI.Configuration;
using SelfBalancingRobot.WebUI.Hubs;
using System;
using System.Device.I2c;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Models;

public class IMUContext : BaseMonitoringContext
{

    public delegate void IMUUpdated(Vector3 YPR, Vector3 gyro, Vector3 acc);
    private Dictionary<int, int> dicPinNoVsLogical = new();
    private Dictionary<int, int> dicLogicalVsPinNo = new();

    private IMUUpdated _OnIMUUpdated;
    public event IMUUpdated OnIMUUpdated
    {
        add
        {
            if (_OnIMUUpdated == null || !_OnIMUUpdated.GetInvocationList().Contains(value))
                _OnIMUUpdated += value;
        }
        remove
        {
            if (_OnIMUUpdated != null && _OnIMUUpdated.GetInvocationList().Contains(value))
                _OnIMUUpdated -= value;
        }
    }

    private readonly DeviceSettings deviceSettings;
    private readonly CalibrationSettings calibSettings;
    private readonly IMUHub imuHub;
    private readonly CalibrationHub calHub;

    public const int MaxHistoryLength = 30;
    private const int MessageFreq = 10;  // Freq Hz

    private int loopDelay = 0;
    private TimeSpan loopDelayTS;
    private TimeSpan messageDelayTS;
    private DCM dcm = new DCM();
    private IIMU imu = null;

    private List<DataHistory> dataHistList = new List<DataHistory>();
    public DataHistory[] GetHistory()
    {
        return dataHistList.ToArray();
    }
    private I2cDevice device = null;

    private Vector3 gyroOffsets = new Vector3();
    private Vector3 accOffsets = new Vector3();

    private DateTime lastMessage = DateTime.Now;
    private DateTime lastRead = DateTime.Now;

    private Vector3 lastYPR = new Vector3();
    private Vector3 lastGyro = new Vector3();
    private Vector3 lastAcc = new Vector3();
    public IMUContext(DeviceSettings deviceSettings, CalibrationSettings calibSettings, IMUHub imuHub, CalibrationHub calHub)
    {
        this.deviceSettings = deviceSettings;
        this.calibSettings = calibSettings;
        this.imuHub = imuHub;
        this.calHub = calHub;
    }

    public Vector3 GetGyro()
    {
        Vector3 data = GetRawGyro();
        data -= gyroOffsets;
        return data;
    }
    public Vector3 GetAccel()
    {
        Vector3 data = GetRawAccel();
        data -= accOffsets;
        return data;
    }

    public Vector3 GetRawGyro()
    {
#if FAKE_IMU
        return new Vector3(gyroLst[currentFakeIndex]);
#else
        return imu.GetGyro();
#endif
    }
    public Vector3 GetRawAccel()
    {
#if FAKE_IMU
        return new Vector3(accLst[currentFakeIndex]);
#else
        return imu.GetAcc();
#endif
    }
    public Vector3 GetYPR()
    {
        return dcm.GetYPR();
    }

    public void Init()
    {
        Console.WriteLine("Begin IMU Init");
        dataHistList.Clear();
        var imuAddress = -1;
        if (!deviceSettings.IMUAddress.HasValue)
        {
            switch (deviceSettings.IMUType)
            {
                case IMUType.MPU6000:
                case IMUType.MPU6050:
                case IMUType.MPU6500:
                case IMUType.MPU9250:
                    imuAddress = Iot.Device.Imu.Mpu6050.DefaultI2cAddress;
                    break;
                case IMUType.MPU6886:
                    imuAddress = Iot.Device.Mpu6886.Mpu6886AccelerometerGyroscope.DefaultI2cAddress;
                    break;
                default:
                    break;
            }
        }
        else
            imuAddress = deviceSettings.IMUAddress.GetValueOrDefault();
        Console.WriteLine($"I2C Bus Id: {deviceSettings.I2CBusId}, I2C Address: {imuAddress}");
#if FAKE_IMU
        LoadFake();
#else
        device = I2cDevice.Create(new I2cConnectionSettings(deviceSettings.I2CBusId, imuAddress));
        switch (deviceSettings.IMUType)
        {
            case IMUType.MPU6000:
            case IMUType.MPU6050:
                imu = new IMU_MPU6050(device);
                break;
            case IMUType.MPU6500:
                imu = new IMU_MPU6500(device);
                break;
            case IMUType.MPU6886:
                imu = new IMU_MPU6886(device);
                break;
            case IMUType.MPU9250:
                imu = new IMU_MPU9250(device);
                break;
            default:
                break;
        }
#endif
        Console.WriteLine("End IMU Init");
    }

    public override void OnStartMonitoring()
    {
        loopDelayTS = TimeSpan.FromMilliseconds(Convert.ToInt32(1000 / deviceSettings.PIDLoopFreq));
        loopDelay = Convert.ToInt32(1000 / deviceSettings.PIDLoopFreq);
        messageDelayTS = TimeSpan.FromMilliseconds(Convert.ToInt32(1000 / MessageFreq));
        dcm.Reset();
        accOffsets = calibSettings.AccelOffsets.ToVector3();
        gyroOffsets = calibSettings.GyroOffsets.ToVector3();
        lastMessage = DateTime.Now;
        lastRead = DateTime.Now;
    }
    public override void MonitoringLoop()
    {
        //DateTime now = DateTime.Now;
        //if ((now - lastRead) < loopDelayTS)
        //    return;
        //base.monitoringTaskToken.Token.WaitHandle.WaitOne()
        base.monitoringTaskToken.Token.WaitHandle.WaitOne(loopDelay);
#if FAKE_IMU
        currentFakeIndex++;
        if (currentFakeIndex >= accLst.Count)
            currentFakeIndex = 0;
#endif
        try
        {
            var rawGyro = GetRawGyro();
            lastGyro = rawGyro - gyroOffsets;
            lastRead = DateTime.Now;
            _OnIMUUpdated?.Invoke(lastYPR, lastGyro, lastAcc);
            if ((lastRead - lastMessage) >= messageDelayTS)
            {
                var rawAcc = GetRawAccel();
                lastAcc = rawAcc - accOffsets;
                UpdateIMU(lastGyro, lastAcc);
                lastYPR = dcm.GetYPR();
                var data = UpdateHistory(lastRead, lastGyro, lastAcc, lastYPR);
                imuHub.SendUpdate(data);
                calHub.SendRawAcc(rawAcc);
                calHub.SendRawGyro(rawGyro);
                lastMessage = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR IMU Error: {ex.Message}");
            Console.WriteLine($"ERROR IMU Trace: {ex.StackTrace}");
            dcm.Reset();
        }
    }
    public void HardwareCalibrate()
    {
        this.StopMonitoring();
#if !FAKE_IMU
        imu.HardwareCalibrate();
#endif
        this.StartMonitoring();
    }

    private void UpdateIMU(Vector3 gyro, Vector3 acc)
    {
        //var time = DateTime.Now;
        dcm.Update(gyro, acc);
        //return UpdateHistory(time, gyro, acc, dcm.GetYPR());
    }
    private DataHistory UpdateHistory(DateTime time, Vector3 gyro, Vector3 acc, Vector3 ypr)
    {
        DataHistory hist = new()
        {
            Time = time,
            Data = new
            {
                acc = new float[] { acc.X, acc.Y, acc.Z },
                gyro = new float[] { gyro.X, gyro.Y, gyro.Z },
                ypr = new float[] { ypr.X, ypr.Y, ypr.Z },
            }
        };
        if (dataHistList.Count >= MaxHistoryLength)
            dataHistList.RemoveAt(0);
        dataHistList.Add(hist);

        return hist;
    }


#if FAKE_IMU
    // for development purpose (run app without IMU)

    private List<float[]> accLst = new List<float[]>();
    private List<float[]> gyroLst = new List<float[]>();
    private int currentFakeIndex = -1;

    private void LoadFake()
    {
        accLst.Add(new float[] { 0.49561745f, 1.8148696f, -9.854886f }); gyroLst.Add(new float[] { -2.7618408f, 0.9384155f, 0.74768066f });
        accLst.Add(new float[] { 0.51237744f, 1.7454354f, -9.917137f }); gyroLst.Add(new float[] { -2.784729f, 0.9613037f, 0.5493164f });
        accLst.Add(new float[] { 0.49801174f, 1.7933211f, -9.881223f }); gyroLst.Add(new float[] { -3.1585693f, 0.6637573f, 0.61798096f });
        accLst.Add(new float[] { 0.47885743f, 1.7909268f, -9.912349f }); gyroLst.Add(new float[] { -3.0975342f, 0.91552734f, 1.1672974f });
        accLst.Add(new float[] { 0.49082887f, 1.7693782f, -9.919532f }); gyroLst.Add(new float[] { -2.8762817f, 0.64849854f, 0.76293945f });
        accLst.Add(new float[] { -5.8468494f, -0.25379443f, -10.841332f }); gyroLst.Add(new float[] { -55.152893f, 17.677307f, 132.87354f });
        accLst.Add(new float[] { 6.119798f, 8.011285f, -0.14605151f }); gyroLst.Add(new float[] { 91.49933f, 35.072327f, 131.68335f });
        accLst.Add(new float[] { -3.4046764f, 0.87391484f, 8.837314f }); gyroLst.Add(new float[] { -49.0036f, 7.873535f, 25.688171f });
        accLst.Add(new float[] { -3.5722764f, 0.2968916f, 9.543629f }); gyroLst.Add(new float[] { 14.36615f, -27.633667f, -50.079346f });
        accLst.Add(new float[] { -5.209969f, 5.387146f, 4.7478714f }); gyroLst.Add(new float[] { 16.571045f, 66.7038f, -4.562378f });
        accLst.Add(new float[] { -3.1077847f, 2.7031503f, -9.141388f }); gyroLst.Add(new float[] { -31.311035f, -13.343811f, 33.302307f });
        accLst.Add(new float[] { -0.124502935f, -0.10774292f, 8.980971f }); gyroLst.Add(new float[] { -90.782166f, -103.66058f, -73.44055f });
        accLst.Add(new float[] { -1.776561f, 0.3926631f, 8.437468f }); gyroLst.Add(new float[] { 81.588745f, 99.739075f, 20.492554f });
        accLst.Add(new float[] { -5.22194f, 4.7215343f, -8.561971f }); gyroLst.Add(new float[] { 61.25641f, -4.5776367f, 62.39319f });
        accLst.Add(new float[] { -0.62251467f, 0.040702883f, 6.155712f }); gyroLst.Add(new float[] { -112.701416f, -79.62036f, -38.07831f });
        accLst.Add(new float[] { -1.5060066f, 5.777415f, -5.018426f }); gyroLst.Add(new float[] { 115.24963f, 203.03345f, -18.78357f });
        accLst.Add(new float[] { -0.63927466f, 5.2650375f, -8.662531f }); gyroLst.Add(new float[] { 28.968811f, -63.591003f, 7.369995f });
        accLst.Add(new float[] { -1.1708064f, 4.5252028f, -9.0504055f }); gyroLst.Add(new float[] { -22.026062f, -6.5612793f, 0.7324219f });
        accLst.Add(new float[] { -0.92658913f, 4.7406883f, -8.858863f }); gyroLst.Add(new float[] { -5.332947f, -4.486084f, 2.0980835f });
        accLst.Add(new float[] { -1.0056006f, 4.4988656f, -9.004914f }); gyroLst.Add(new float[] { -0.2670288f, -3.0288696f, 0.7171631f });
        accLst.Add(new float[] { -0.99362916f, 4.4845f, -8.95224f }); gyroLst.Add(new float[] { -5.0354004f, 1.121521f, 1.9302368f });
        accLst.Add(new float[] { -0.967292f, 4.4749227f, -9.00252f }); gyroLst.Add(new float[] { -2.632141f, 0.2822876f, 0.9460449f });
        accLst.Add(new float[] { -0.9601092f, 4.410277f, -9.024068f }); gyroLst.Add(new float[] { -2.4719238f, 0.90026855f, 1.4190674f });
        accLst.Add(new float[] { -0.93856055f, 4.4318256f, -9.062377f }); gyroLst.Add(new float[] { -2.922058f, 1.1672974f, 1.1749268f });
        accLst.Add(new float[] { -0.96250343f, 4.410277f, -8.980971f }); gyroLst.Add(new float[] { -2.3880005f, 1.1367798f, 1.159668f });
        accLst.Add(new float[] { -0.9313777f, 4.3743625f, -9.0504055f }); gyroLst.Add(new float[] { -2.5787354f, 0.9613037f, 0.7171631f });
        accLst.Add(new float[] { -0.90982914f, 4.36718f, -9.031251f }); gyroLst.Add(new float[] { -2.822876f, 0.9384155f, 0.8468628f });
        accLst.Add(new float[] { -0.90264624f, 4.3576026f, -9.048011f }); gyroLst.Add(new float[] { -3.074646f, 0.9765625f, 0.8544922f });
        accLst.Add(new float[] { -0.8715205f, 4.343237f, -9.107868f }); gyroLst.Add(new float[] { -2.067566f, 1.0910034f, 0.75531006f });
        accLst.Add(new float[] { -0.8882805f, 4.3240824f, -9.105474f }); gyroLst.Add(new float[] { -2.822876f, 1.3046265f, 0.9841919f });
        accLst.Add(new float[] { -0.9289834f, 4.2833796f, -9.079137f }); gyroLst.Add(new float[] { -2.8839111f, 0.77819824f, 0.6561279f });
        accLst.Add(new float[] { -0.8906748f, 4.3169f, -9.08632f }); gyroLst.Add(new float[] { -2.7618408f, 1.0375977f, 0.77056885f });
        accLst.Add(new float[] { -0.8906748f, 4.3073225f, -9.024068f }); gyroLst.Add(new float[] { -2.5100708f, 0.7171631f, 0.6942749f });
        accLst.Add(new float[] { -0.8882805f, 4.295351f, -9.11984f }); gyroLst.Add(new float[] { -2.5177002f, 0.8773804f, 0.8773804f });
        accLst.Add(new float[] { -0.88588625f, 4.3073225f, -9.091108f }); gyroLst.Add(new float[] { -3.1738281f, 0.831604f, 0.7095337f });
        accLst.Add(new float[] { -0.90264624f, 4.30014f, -9.100685f }); gyroLst.Add(new float[] { -2.7160645f, 0.77819824f, 0.93078613f });
        accLst.Add(new float[] { -0.8715205f, 4.2642255f, -9.098291f }); gyroLst.Add(new float[] { -2.9144287f, 0.9765625f, 1.121521f });
        accLst.Add(new float[] { -0.88349193f, 4.292957f, -9.074348f }); gyroLst.Add(new float[] { -2.8381348f, 1.0986328f, 0.8773804f });
        accLst.Add(new float[] { -0.90504056f, 4.2833796f, -9.0528f }); gyroLst.Add(new float[] { -3.0212402f, 0.8163452f, 0.7019043f });
        accLst.Add(new float[] { -0.86673194f, 4.2594366f, -9.105474f }); gyroLst.Add(new float[] { -2.571106f, 0.7247925f, 0.64849854f });
        accLst.Add(new float[] { -0.87870336f, 4.302534f, -9.071954f }); gyroLst.Add(new float[] { -2.7160645f, 0.93078613f, 0.7095337f });
        accLst.Add(new float[] { -0.8763091f, 4.254648f, -9.059982f }); gyroLst.Add(new float[] { -3.0822754f, 0.869751f, 0.8621216f });
        accLst.Add(new float[] { -0.8595491f, 4.2618313f, -9.100685f }); gyroLst.Add(new float[] { -2.861023f, 0.78582764f, 0.47302246f });
        accLst.Add(new float[] { -0.9146177f, 4.254648f, -9.057589f }); gyroLst.Add(new float[] { -3.1967163f, 0.9536743f, 0.7247925f });
        accLst.Add(new float[] { -0.9241948f, 4.2235227f, -9.122234f }); gyroLst.Add(new float[] { -2.960205f, 0.75531006f, 0.44250488f });
        accLst.Add(new float[] { -0.9122234f, 4.2378883f, -9.088714f }); gyroLst.Add(new float[] { -2.9449463f, 0.61035156f, 0.6713867f });
        accLst.Add(new float[] { -0.88588625f, 4.2307053f, -9.083925f }); gyroLst.Add(new float[] { -2.6550293f, 0.9460449f, 0.6942749f });
        accLst.Add(new float[] { -0.8380005f, 4.2570424f, -9.08632f }); gyroLst.Add(new float[] { -2.7923584f, 0.88500977f, 0.8163452f });
        accLst.Add(new float[] { -0.86912626f, 4.228311f, -9.143783f }); gyroLst.Add(new float[] { -2.7999878f, 1.0910034f, 0.61035156f });
        accLst.Add(new float[] { -0.84278905f, 4.245071f, -9.143783f }); gyroLst.Add(new float[] { -2.89917f, 1.2893677f, 0.7171631f });
        accLst.Add(new float[] { -0.8547605f, 4.201974f, -9.134206f }); gyroLst.Add(new float[] { -2.5253296f, 0.7247925f, 0.5569458f });
        accLst.Add(new float[] { -0.90504056f, 4.2307053f, -9.110263f }); gyroLst.Add(new float[] { -2.8076172f, 0.5493164f, 0.51116943f });
        accLst.Add(new float[] { -0.87391484f, 4.235494f, -9.112657f }); gyroLst.Add(new float[] { -2.0599365f, 0.90026855f, 0.79345703f });
        accLst.Add(new float[] { -0.90504056f, 4.24986f, -9.083925f }); gyroLst.Add(new float[] { -2.9754639f, 1.0375977f, 0.6866455f });
        accLst.Add(new float[] { -0.86912626f, 4.235494f, -9.112657f }); gyroLst.Add(new float[] { -2.670288f, 0.8239746f, 0.89263916f });
        accLst.Add(new float[] { -0.87391484f, 4.2426767f, -9.148571f }); gyroLst.Add(new float[] { -3.0670166f, 1.0299683f, 0.89263916f });
        accLst.Add(new float[] { -0.8906748f, 4.2570424f, -9.107868f }); gyroLst.Add(new float[] { -2.746582f, 1.083374f, 0.579834f });
        accLst.Add(new float[] { -0.8930691f, 4.2426767f, -9.093503f }); gyroLst.Add(new float[] { -2.7923584f, 1.045227f, 0.74768066f });
        accLst.Add(new float[] { -0.8284234f, 4.2378883f, -9.127023f }); gyroLst.Add(new float[] { -2.5558472f, 0.78582764f, 0.5493164f });
        accLst.Add(new float[] { -0.88349193f, 4.218734f, -9.091108f }); gyroLst.Add(new float[] { -2.5787354f, 1.0528564f, 0.4348755f });
        accLst.Add(new float[] { -0.8715205f, 4.2235227f, -9.043222f }); gyroLst.Add(new float[] { -3.112793f, 0.9689331f, 0.93078613f });
        accLst.Add(new float[] { -0.8451834f, 4.218734f, -9.146177f }); gyroLst.Add(new float[] { -2.9754639f, 0.8010864f, 0.75531006f });
        accLst.Add(new float[] { -0.8236348f, 4.2642255f, -9.127023f }); gyroLst.Add(new float[] { -3.2196045f, 0.9689331f, 0.7095337f });
        accLst.Add(new float[] { -0.86194336f, 4.225917f, -9.184485f }); gyroLst.Add(new float[] { -2.6779175f, 0.8239746f, 0.75531006f });
        accLst.Add(new float[] { -0.8547605f, 4.218734f, -9.1366f }); gyroLst.Add(new float[] { -3.0975342f, 1.0070801f, 0.541687f });
        accLst.Add(new float[] { -0.8643377f, 4.2378883f, -9.110263f }); gyroLst.Add(new float[] { -2.9754639f, 0.9384155f, 0.4119873f });
        accLst.Add(new float[] { -0.88349193f, 4.2426767f, -9.076742f }); gyroLst.Add(new float[] { -3.0059814f, 0.831604f, 0.831604f });
        accLst.Add(new float[] { -0.8523662f, 4.252254f, -9.105474f }); gyroLst.Add(new float[] { -2.746582f, 0.9384155f, 0.64086914f });
        accLst.Add(new float[] { -0.8595491f, 4.218734f, -9.112657f }); gyroLst.Add(new float[] { -2.9678345f, 0.9841919f, 0.61035156f });
        accLst.Add(new float[] { -0.87391484f, 4.235494f, -9.112657f }); gyroLst.Add(new float[] { -2.5787354f, 0.7019043f, 0.6942749f });
        accLst.Add(new float[] { -0.84997195f, 4.2378883f, -9.107868f }); gyroLst.Add(new float[] { -2.708435f, 1.0681152f, 0.49591064f });
        accLst.Add(new float[] { -0.8284234f, 4.235494f, -9.138994f }); gyroLst.Add(new float[] { -2.6168823f, 1.121521f, 0.75531006f });
        accLst.Add(new float[] { -0.8356062f, 4.225917f, -9.141388f }); gyroLst.Add(new float[] { -2.4337769f, 1.3198853f, 0.5645752f });
        accLst.Add(new float[] { -0.84278905f, 4.2666197f, -9.1366f }); gyroLst.Add(new float[] { -2.9678345f, 1.121521f, 0.78582764f });
        accLst.Add(new float[] { -0.8164519f, 4.254648f, -9.093503f }); gyroLst.Add(new float[] { -3.0441284f, 0.9841919f, 0.8087158f });
        accLst.Add(new float[] { -0.79490334f, 4.180425f, -9.088714f }); gyroLst.Add(new float[] { -2.708435f, 0.7019043f, 0.9460449f });
        accLst.Add(new float[] { -0.86673194f, 4.2163396f, -9.0528f }); gyroLst.Add(new float[] { -2.8305054f, 0.9384155f, 0.6866455f });
        accLst.Add(new float[] { -0.8212405f, 4.204368f, -9.1366f }); gyroLst.Add(new float[] { -2.8305054f, 0.9460449f, 0.51116943f });
        accLst.Add(new float[] { -0.82602906f, 4.2139454f, -9.055194f }); gyroLst.Add(new float[] { -2.8381348f, 0.9765625f, 0.77056885f });
        accLst.Add(new float[] { -0.86194336f, 4.211551f, -9.093503f }); gyroLst.Add(new float[] { -3.0288696f, 1.0147095f, 0.5187988f });
        accLst.Add(new float[] { -0.8763091f, 4.225917f, -9.1366f }); gyroLst.Add(new float[] { -2.7542114f, 0.8163452f, 0.88500977f });
        accLst.Add(new float[] { -0.8571548f, 4.2235227f, -9.124628f }); gyroLst.Add(new float[] { -3.1661987f, 0.8468628f, 0.4348755f });
        accLst.Add(new float[] { -0.86912626f, 4.2307053f, -9.095897f }); gyroLst.Add(new float[] { -3.0136108f, 0.9384155f, 0.4348755f });
        accLst.Add(new float[] { -0.8403948f, 4.1971855f, -9.074348f }); gyroLst.Add(new float[] { -3.0136108f, 0.89263916f, 0.44250488f });
        accLst.Add(new float[] { -0.86194336f, 4.2163396f, -9.098291f }); gyroLst.Add(new float[] { -3.4255981f, 0.90789795f, 0.79345703f });
        accLst.Add(new float[] { -0.84757763f, 4.235494f, -9.11984f }); gyroLst.Add(new float[] { -3.0670166f, 1.0681152f, 0.579834f });
        accLst.Add(new float[] { -0.8595491f, 4.218734f, -9.081532f }); gyroLst.Add(new float[] { -3.1204224f, 0.61035156f, 0.38909912f });
        accLst.Add(new float[] { -0.8643377f, 4.235494f, -9.138994f }); gyroLst.Add(new float[] { -3.1738281f, 1.1901855f, 0.8010864f });
        accLst.Add(new float[] { -0.86194336f, 4.1971855f, -9.091108f }); gyroLst.Add(new float[] { -2.5405884f, 0.93078613f, 0.61798096f });
        accLst.Add(new float[] { -0.8403948f, 4.2067623f, -9.100685f }); gyroLst.Add(new float[] { -3.2424927f, 0.60272217f, 0.76293945f });
        accLst.Add(new float[] { -0.8547605f, 4.18282f, -9.1366f }); gyroLst.Add(new float[] { -2.6779175f, 0.79345703f, 0.8773804f });
        accLst.Add(new float[] { -0.86194336f, 4.18282f, -9.177302f }); gyroLst.Add(new float[] { -2.8533936f, 0.90789795f, 0.76293945f });
        accLst.Add(new float[] { -0.8188462f, 4.2402825f, -9.138994f }); gyroLst.Add(new float[] { -3.5171509f, 1.6479492f, 0.7171631f });
        accLst.Add(new float[] { -0.8212405f, 4.2163396f, -9.107868f }); gyroLst.Add(new float[] { -3.2196045f, 0.8621216f, 0.9613037f });
        accLst.Add(new float[] { -0.86912626f, 4.2235227f, -9.10308f }); gyroLst.Add(new float[] { -2.632141f, 0.64849854f, 0.6637573f });
        accLst.Add(new float[] { -0.8643377f, 4.1947913f, -9.127023f }); gyroLst.Add(new float[] { -2.746582f, 1.3809204f, 0.5645752f });
        accLst.Add(new float[] { -0.8236348f, 4.211551f, -9.093503f }); gyroLst.Add(new float[] { -2.9067993f, 1.3427734f, 0.93078613f });
        accLst.Add(new float[] { -0.8332119f, 4.2235227f, -9.098291f }); gyroLst.Add(new float[] { -2.5863647f, 0.8163452f, 0.8163452f });
        accLst.Add(new float[] { -0.8643377f, 4.204368f, -9.131811f }); gyroLst.Add(new float[] { -2.8533936f, 0.79345703f, 0.8163452f });
        accLst.Add(new float[] { -0.8451834f, 4.1995797f, -9.105474f }); gyroLst.Add(new float[] { -2.8152466f, 1.0223389f, 0.76293945f });
        accLst.Add(new float[] { -0.84278905f, 4.2307053f, -9.08632f }); gyroLst.Add(new float[] { -2.8381348f, 0.75531006f, 0.4196167f });
        accLst.Add(new float[] { -0.8164519f, 4.2067623f, -9.131811f }); gyroLst.Add(new float[] { -2.7999878f, 1.2207031f, 0.7171631f });
        accLst.Add(new float[] { -0.8643377f, 4.204368f, -9.131811f }); gyroLst.Add(new float[] { -2.8076172f, 0.8163452f, 0.7095337f });
        accLst.Add(new float[] { -0.84997195f, 4.185214f, -9.071954f }); gyroLst.Add(new float[] { -2.9754639f, 0.93078613f, 0.5722046f });
        accLst.Add(new float[] { -0.8284234f, 4.204368f, -9.150966f }); gyroLst.Add(new float[] { -3.3569336f, 0.8163452f, 0.74768066f });
        accLst.Add(new float[] { -0.8380005f, 4.1900024f, -9.117445f }); gyroLst.Add(new float[] { -3.2577515f, 0.79345703f, 0.61798096f });
        accLst.Add(new float[] { -0.8595491f, 4.175637f, -9.093503f }); gyroLst.Add(new float[] { -2.7008057f, 0.8010864f, 0.90026855f });
        accLst.Add(new float[] { -0.87391484f, 4.1900024f, -9.083925f }); gyroLst.Add(new float[] { -2.456665f, 1.2588501f, 0.7095337f });
        accLst.Add(new float[] { -0.8451834f, 4.2163396f, -9.038434f }); gyroLst.Add(new float[] { -2.6474f, 1.0147095f, 0.6637573f });
        accLst.Add(new float[] { -0.7972976f, 4.1971855f, -9.17012f }); gyroLst.Add(new float[] { -2.7542114f, 1.045227f, 0.6790161f });
        accLst.Add(new float[] { -0.8810977f, 4.2474656f, -9.08632f }); gyroLst.Add(new float[] { -2.6550293f, 1.2207031f, 0.75531006f });
        accLst.Add(new float[] { -0.84278905f, 4.1900024f, -9.129416f }); gyroLst.Add(new float[] { -3.2577515f, 0.6637573f, 0.5340576f });
        accLst.Add(new float[] { -0.84997195f, 4.2307053f, -9.071954f }); gyroLst.Add(new float[] { -2.7236938f, 0.90026855f, 0.60272217f });
        accLst.Add(new float[] { -0.8212405f, 4.225917f, -9.081532f }); gyroLst.Add(new float[] { -2.960205f, 1.0681152f, 0.8239746f });
        accLst.Add(new float[] { -0.8571548f, 4.1923966f, -9.093503f }); gyroLst.Add(new float[] { -2.9449463f, 1.2664795f, 0.60272217f });
        accLst.Add(new float[] { -0.8547605f, 4.2163396f, -9.107868f }); gyroLst.Add(new float[] { -3.5095215f, 0.7171631f, 0.8163452f });
        accLst.Add(new float[] { -0.8212405f, 4.2378883f, -9.057589f }); gyroLst.Add(new float[] { -3.3569336f, 0.6637573f, 0.8087158f });
        accLst.Add(new float[] { -0.8571548f, 4.211551f, -9.105474f }); gyroLst.Add(new float[] { -2.8686523f, 0.6561279f, 0.8239746f });
        accLst.Add(new float[] { -0.80687475f, 4.1971855f, -9.081532f }); gyroLst.Add(new float[] { -3.0059814f, 0.79345703f, 0.6561279f });
        accLst.Add(new float[] { -0.8523662f, 4.225917f, -9.122234f }); gyroLst.Add(new float[] { -3.1433105f, 1.197815f, 0.8010864f });
        accLst.Add(new float[] { -0.8116633f, 4.218734f, -9.127023f }); gyroLst.Add(new float[] { -2.8076172f, 1.1520386f, 0.35095215f });
        accLst.Add(new float[] { -0.84997195f, 4.2067623f, -9.127023f }); gyroLst.Add(new float[] { -2.4337769f, 1.1444092f, 0.61798096f });
        accLst.Add(new float[] { -0.8236348f, 4.2211285f, -9.079137f }); gyroLst.Add(new float[] { -3.0212402f, 1.1444092f, 0.64086914f });
        accLst.Add(new float[] { -0.8284234f, 4.2474656f, -9.1605425f }); gyroLst.Add(new float[] { -2.6474f, 0.91552734f, 0.64849854f });
        accLst.Add(new float[] { -0.8092691f, 4.1923966f, -9.083925f }); gyroLst.Add(new float[] { -2.5405884f, 1.1901855f, 0.77056885f });
        accLst.Add(new float[] { -0.8164519f, 4.228311f, -9.146177f }); gyroLst.Add(new float[] { -2.8915405f, 1.1825562f, 0.6866455f });
        accLst.Add(new float[] { -0.83081764f, 4.178031f, -9.155754f }); gyroLst.Add(new float[] { -2.2277832f, 1.0147095f, 0.61035156f });
        accLst.Add(new float[] { -0.83081764f, 4.1900024f, -9.122234f }); gyroLst.Add(new float[] { -3.0593872f, 1.159668f, 0.90026855f });
    }
#endif
}
