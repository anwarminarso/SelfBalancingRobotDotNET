using Iot.Device.Imu;
using Iot.Device.Mpu6886;
using System.Device.I2c;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Models;


public enum IMUType
{
    MPU6000,
    MPU6050,
    MPU6500,
    MPU6886,
    MPU9250,
}
public interface IIMU
{
    Vector3 GetAcc();
    Vector3 GetGyro();
}

public abstract class IMU<T> : IIMU
    where T : class
{
    protected I2cDevice device;
    protected T imu;

    public abstract IMUType imuType { get; }

    public IMU(I2cDevice device)
    {
        this.device = device;
    }
    public abstract Vector3 GetAcc();
    public abstract Vector3 GetGyro();
}

public class IMU_MPU6886 : IMU<Mpu6886AccelerometerGyroscope>
{
    public override IMUType imuType => IMUType.MPU6886;

    public IMU_MPU6886(I2cDevice device)
        : base(device)
    {
        this.device = device;
        this.imu = new Mpu6886AccelerometerGyroscope(device);
    }
    public override Vector3 GetAcc()
    {
        return imu.GetAccelerometer();
    }
    public override Vector3 GetGyro()
    {
        return imu.GetGyroscope();
    }
}

public class IMU_MPU6050 : IMU<Mpu6050>
{
    public override IMUType imuType => IMUType.MPU6050;

    public IMU_MPU6050(I2cDevice device)
        : base(device)
    {
        this.device = device;
        this.imu = new Mpu6050(device);
    }
    public override Vector3 GetAcc()
    {
        return imu.GetAccelerometer();
    }
    public override Vector3 GetGyro()
    {
        return imu.GetGyroscopeReading();
    }
}

public class IMU_MPU6500 : IMU<Mpu6500>
{
    public override IMUType imuType => IMUType.MPU6500;

    public IMU_MPU6500(I2cDevice device)
        : base(device)
    {
        this.device = device;
        this.imu = new Mpu6500(device);
    }
    public override Vector3 GetAcc()
    {
        return imu.GetAccelerometer();
    }
    public override Vector3 GetGyro()
    {
        return imu.GetGyroscopeReading();
    }
}

public class IMU_MPU9250 : IMU<Mpu9250>
{
    public override IMUType imuType => IMUType.MPU9250;

    public IMU_MPU9250(I2cDevice device)
        : base(device)
    {
        this.device = device;
        this.imu = new Mpu9250(device);
    }
    public override Vector3 GetAcc()
    {
        return imu.GetAccelerometer();
    }
    public override Vector3 GetGyro()
    {
        return imu.GetGyroscopeReading();
    }
}

