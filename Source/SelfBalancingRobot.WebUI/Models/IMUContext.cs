using Iot.Device.Mcp25xxx.Register;
using Iot.Device.SensorHub;
using SelfBalancingRobot.WebUI.Configuration;
using SelfBalancingRobot.WebUI.Hubs;
using System;
using System.Device.I2c;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Models;

public class IMUContext
{
    private readonly AppSettings appSettings;
    private readonly IMUHub imuHub;

    private const int MaxHistoryLength = 200;
    private const int MaxHistoryFreq = 20;  // Freq Hz

    private Task monitoringTask;
    private CancellationTokenSource monitoringTaskToken;
    private DCM dcm = new DCM();
    IIMU imu;

    private List<DataHistory> dataHistList = new List<DataHistory>();
    public DataHistory[] GetHistory()
    {
        return dataHistList.ToArray();
    }
    private I2cDevice device = null;

    public IMUContext(AppSettings appSettings, IMUHub imuHub)
    {
        this.appSettings = appSettings;
        this.imuHub = imuHub;
    }

    public Vector3 GetGyro()
    {
        return imu.GetGyro();
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
        if (!appSettings.IMUAddress.HasValue)
        {
            switch (appSettings.IMUType)
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
            imuAddress = appSettings.IMUAddress.GetValueOrDefault();
        Console.WriteLine($"I2C Bus Id: {appSettings.I2CBusId}, I2C Address: {imuAddress}");
        device = I2cDevice.Create(new I2cConnectionSettings(appSettings.I2CBusId, imuAddress));
        switch (appSettings.IMUType)
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
        Console.WriteLine("End IMU Init");
    }

    public void StartMonitoring()
    {
        if (monitoringTask == null || monitoringTask.IsCanceled)
        {
            monitoringTaskToken = new CancellationTokenSource();
            var delay = Convert.ToInt32(1000 / MaxHistoryFreq);
            monitoringTask = new Task(() =>
            {
                while (!monitoringTaskToken.IsCancellationRequested)
                {
                    Utils.Delay(delay, false);
                    monitoringTaskToken.Token.WaitHandle.WaitOne(delay);
                    var data = UpdateIMU();
                    imuHub.SendUpdate(data);
                    //_OnDiagUpdated?.Invoke(this);
                }
            }, monitoringTaskToken.Token);
            monitoringTask.Start();
        }
    }

    public void StopMonitoring()
    {

        if (monitoringTaskToken != null)
        {
            monitoringTaskToken.Cancel();
            while (monitoringTask.Status == TaskStatus.Running)
            {
            }
            if (monitoringTask != null)
            {
                monitoringTask.Dispose();
                monitoringTask = null;
            }
            monitoringTaskToken.Dispose();
            monitoringTaskToken = null;
        }
    }
    private DataHistory UpdateIMU()
    {
        var acc = imu.GetAcc();
        var gyro = imu.GetGyro();
        var time = DateTime.Now;

        dcm.Update(gyro, acc);
        return UpdateHistory(time, gyro, acc, dcm.GetYPR());
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
}
