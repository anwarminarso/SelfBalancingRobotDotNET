using SelfBalancingRobot.WebUI.Configuration;
using System;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Models;

public class StabilizerContext
{
    private readonly IMUContext imuContext;
    private readonly ControlContext controlContext;
    private readonly CalibrationSettings calibrationSettings;
    private readonly MotorContext motorContext;

    private bool stabilzerOn = false;
    private DateTime lastTime;
    private DateTime currentTime;
    private long dt;
    private int failsafeCounter;
    private bool rcOnline;
    private float[] MotorSpeed = new float[2];

    private float error, errorAngle;
    private float delta;
    private float gyroTemp;
    private Vector3 lastGyro;
    private Vector3 delta1;
    private Vector3 delta2;
    private float errorAngleI;
    private Vector3 errorGyroI;
    private float PTerm, ITerm, DTerm;
    private Vector3 axisPID;
    private float rc;
    private float deltaDeg;
    private float lastDeg;
    private float deltaDeg1, deltaDeg2;
    private bool isStarted = false;


    public StabilizerContext(IMUContext imuContext, CalibrationSettings calibrationSettings, ControlContext controlContext, MotorContext motorContext)
    {
        this.imuContext = imuContext;
        this.controlContext = controlContext;
        this.calibrationSettings = calibrationSettings;
        this.motorContext = motorContext;
    }

    public void Reset()
    {
        stabilzerOn = false;
        rcOnline = false;
        errorAngleI = 0;
        errorGyroI = new Vector3();
        error = 0;

        lastGyro = new Vector3();
        delta2 = new Vector3();
        delta1 = new Vector3();

        lastDeg = 0;
        deltaDeg1 = 0;
        deltaDeg2 = 0;
        

    }

    private void Stabilize()
    {
        if (!isStarted)
        {
            Reset();
            isStarted = true;
            currentTime = DateTime.Now;
        }
        lastTime = currentTime;
        currentTime = DateTime.Now;

        dt = Convert.ToInt64((currentTime - lastTime).TotalMilliseconds);
        if (rcOnline && failsafeCounter > 250)
        {
            controlContext.ControlX = 0.0f;
            controlContext.ControlY = 0.0f;
            failsafeCounter = 0;
            rcOnline = false;
        }
        else if (rcOnline)
        {
            failsafeCounter++;
        }
        else
        {
            controlContext.ControlX = 0.0f;
            controlContext.ControlY = 0.0f;
        }


        var ypr = imuContext.GetYPR();
        var gyro = imuContext.GetGyro();

        #region PITCH
        rc = controlContext.ControlY * 0.1f;
        deltaDeg = ypr[1] - lastDeg;
        deltaDeg = deltaDeg * dt / 1000.0f;

        errorAngle = rc - ypr[1];
        PTerm = errorAngle * calibrationSettings.AnglePID.P;

        errorAngleI = Utils.constrain(errorAngleI + errorAngle, -90.0f, +90.0f);
        ITerm = errorAngleI * calibrationSettings.AnglePID.I;
        DTerm = (deltaDeg1 + deltaDeg2 + deltaDeg) / 3.0f;
        DTerm = DTerm * calibrationSettings.AnglePID.D;

        lastDeg = ypr[1];
        deltaDeg2 = deltaDeg1;
        deltaDeg1 = deltaDeg;
        rc = (PTerm + ITerm - DTerm);
        /*Serial.print(", RC PID: ");
        Serial.print(rc);*/

        gyroTemp = gyro.Y; // y axis
        error = rc - gyroTemp;
        delta = gyroTemp - lastGyro.Y;
        delta = delta * dt / 1000.0f;

        PTerm = error * calibrationSettings.PitchPID.P; // P

        errorGyroI[1] = Utils.constrain(errorGyroI[1] + error, -500.0f, +500.0f); //max 500 deg/s;
        ITerm = errorGyroI[1] * calibrationSettings.PitchPID.I; //I;

        DTerm = (delta1[1] + delta2[1] + delta) / 3.0f;
        DTerm = DTerm * calibrationSettings.PitchPID.D; //D;

        delta2[1] = delta1[1];
        delta1[1] = delta;
        axisPID[1] = (PTerm + ITerm - DTerm);
        #endregion

        #region Yaw
        rc = controlContext.ControlX;
        gyroTemp = gyro[2]; // z axis
        error = rc - gyroTemp;
        delta = gyroTemp - lastGyro[2];
        delta = delta * dt / 1000.0f;

        PTerm = error * calibrationSettings.YawPID.P; // P

        errorGyroI[2] = Utils.constrain(errorGyroI[2] + error, -500.0f, +500.0f); //max 500 deg/s;
        ITerm = errorGyroI[2] * calibrationSettings.YawPID.I; //I;

        DTerm = (delta1[2] + delta2[2] + delta) / 3;
        DTerm = DTerm * calibrationSettings.YawPID.D; //D;

        delta2[2] = delta1[2];
        delta1[2] = delta;
        axisPID[2] = (PTerm + ITerm - DTerm);
        #endregion


        lastGyro = gyro;

        MotorSpeed[0] = Utils.constrain(PIDMix(0, -1, -1), -4095, 4095);
        MotorSpeed[1] = Utils.constrain(PIDMix(0, -1, 1), -4095, 4095);

        var leftSpeed = Utils.map(MotorSpeed[0], -4095, 4095, -1000, 1000);
        var rightSpeed = Utils.map(MotorSpeed[1], -4095, 4095, -1000, 1000);
        motorContext.Drive(leftSpeed, rightSpeed);
    }

    private float PIDMix(int X, int Y, int Z)
    {
        return axisPID[0] * X + axisPID[1] * Y + axisPID[2] * Z;
    }
}
