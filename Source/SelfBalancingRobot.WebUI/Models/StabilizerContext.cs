using SelfBalancingRobot.WebUI.Configuration;
using System;
using System.Numerics;

namespace SelfBalancingRobot.WebUI.Models;

public class StabilizerContext
{
    private readonly CalibrationSettings calibrationSettings;


    public bool RCOnline { get; set; }
    private DateTime lastTime;
    private DateTime currentTime;
    private long dt;
    private int failsafeCounter;
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


    public StabilizerContext(CalibrationSettings calibrationSettings)
    {
        this.calibrationSettings = calibrationSettings;
    }

    public void Reset()
    {
        RCOnline = false;
        errorAngleI = 0;
        errorGyroI = new Vector3();
        error = 0;

        lastGyro = new Vector3();
        delta2 = new Vector3();
        delta1 = new Vector3();

        lastDeg = 0;
        deltaDeg1 = 0;
        deltaDeg2 = 0;
        currentTime = DateTime.Now;
    }

    public (float leftSpeed, float rightSpeed) Stabilize(Vector3 gyro, Vector3 ypr, float controlX, float controlY)
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
        if (RCOnline && failsafeCounter > 250)
        {
            controlX = 0.0f;
            controlY = 0.0f;
            failsafeCounter = 0;
            RCOnline = false;
        }
        else if (RCOnline)
        {
            failsafeCounter++;
        }
        else
        {
            controlX = 0.0f;
            controlY = 0.0f;
        }

        #region PITCH
        rc = controlY * 0.1f;
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
        rc = controlX;
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

        return (leftSpeed, rightSpeed);
    }

    private float PIDMix(int X, int Y, int Z)
    {
        return axisPID[0] * X + axisPID[1] * Y + axisPID[2] * Z;
    }


}
