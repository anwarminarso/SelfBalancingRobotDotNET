using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace SelfBalancingRobot.WebUI.Models;

public class DCM
{
    float Kp_ROLLPITCH = 1.2f;
    float Ki_ROLLPITCH = 0.00002f;
    float Kp_YAW = 1.2f;
    float Ki_YAW = 0.00002f;
    float G_Dt = 0f;
    float pi = Convert.ToSingle(Math.PI);

    DateTime timestamp_old;
    DateTime timestamp;
    bool isStarted = false;
    Vector3 Accel_Vector = new Vector3();
    Vector3 Gyro_Vector = new Vector3();
    Vector3 YPR = new Vector3();

    Vector3 Omega_Vector = new Vector3();
    Vector3 Omega_P = new Vector3();
    Vector3 Omega_I = new Vector3();
    Vector3 Omega = new Vector3();

    Vector3 Scaled_Omega_I = new Vector3();
    Vector3 errorRollPitch = new Vector3();
    Vector3 errorYaw = new Vector3();
    Vector3[] DCM_Matrix = new Vector3[3] { new Vector3(), new Vector3(), new Vector3() };
    Vector3[] Update_Matrix = new Vector3[3] { new Vector3(), new Vector3(), new Vector3() };
    Vector3[] Temporary_Matrix = new Vector3[3] { new Vector3(), new Vector3(), new Vector3() };

    public DCM()
    {
        //var test = new float[] { 0.49561745f, 1.8148696f, -9.854886f };

    }
    public void Reset()
    {
        YPR[0] = 0;
        YPR[1] = 0;
        YPR[2] = 0;

        Omega_Vector[0] = 0;
        Omega_Vector[1] = 0;
        Omega_Vector[2] = 0;

        Omega_P[0] = 0;
        Omega_P[1] = 0;
        Omega_P[2] = 0;

        Omega_I[0] = 0;
        Omega_I[1] = 0;
        Omega_I[2] = 0;

        Omega[0] = 0;
        Omega[1] = 0;
        Omega[2] = 0;

        errorRollPitch[0] = 0;
        errorRollPitch[1] = 0;
        errorRollPitch[2] = 0;

        errorYaw[0] = 0;
        errorYaw[1] = 0;
        errorYaw[2] = 0;

        DCM_Matrix[0][0] = 1;
        DCM_Matrix[0][1] = 0;
        DCM_Matrix[0][2] = 0;
        DCM_Matrix[1][0] = 0;
        DCM_Matrix[1][1] = 1;
        DCM_Matrix[1][2] = 0;
        DCM_Matrix[2][0] = 0;
        DCM_Matrix[2][1] = 0;
        DCM_Matrix[2][2] = 1;

        Update_Matrix[0][0] = 0;
        Update_Matrix[0][1] = 1;
        Update_Matrix[0][2] = 2;
        Update_Matrix[1][0] = 3;
        Update_Matrix[1][1] = 4;
        Update_Matrix[1][2] = 5;
        Update_Matrix[2][0] = 6;
        Update_Matrix[2][1] = 7;
        Update_Matrix[2][2] = 8;

        Temporary_Matrix[0][0] = 0;
        Temporary_Matrix[0][1] = 0;
        Temporary_Matrix[0][2] = 0;
        Temporary_Matrix[1][0] = 0;
        Temporary_Matrix[1][1] = 0;
        Temporary_Matrix[1][2] = 0;
        Temporary_Matrix[2][0] = 0;
        Temporary_Matrix[2][1] = 0;
        Temporary_Matrix[2][2] = 0;

        isStarted = false;
        timestamp_old = DateTime.Now;
        timestamp = DateTime.Now;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="gyro">Gyro vector in radians</param>
    /// <param name="acc">Accelero vector in </param>
    public void Update(Vector3 gyro, Vector3 acc)
    {
        if (!isStarted)
        {
            Reset();
            isStarted = true;
            timestamp = DateTime.Now;
        }
        timestamp_old = timestamp;
        timestamp = DateTime.Now;
        if (timestamp > timestamp_old)
        {
            var ts = timestamp - timestamp_old;
            G_Dt = Convert.ToSingle(ts.TotalMilliseconds) / 1000.0f; // Real time of loop run. We use this on the DCM algorithm (gyro integration time)
        }
        else
            G_Dt = 0;
        //Console.WriteLine("Dt: " + G_Dt.ToString());
        Gyro_Vector[0] = pi * gyro[0] / 180.0f; //gyro x roll
        Gyro_Vector[1] = pi * gyro[1] / 180.0f; //gyro y pitch
        Gyro_Vector[2] = pi * gyro[2] / 180.0f; //gyro z yaw
        Accel_Vector[0] = acc[0] / 9.80665f;
        Accel_Vector[1] = acc[1] / 9.80665f;
        Accel_Vector[2] = acc[2] / 9.80665f;
        Omega = Gyro_Vector + Omega_I; //adding proportional term
        Omega_Vector = Omega + Omega_P; //adding Integrator term

        Update_Matrix[0][0] = 0;
        Update_Matrix[0][1] = -G_Dt * Omega_Vector[2];//-z
        Update_Matrix[0][2] = G_Dt * Omega_Vector[1];//y
        Update_Matrix[1][0] = G_Dt * Omega_Vector[2];//z
        Update_Matrix[1][1] = 0;
        Update_Matrix[1][2] = -G_Dt * Omega_Vector[0];//-x
        Update_Matrix[2][0] = -G_Dt * Omega_Vector[1];//-y
        Update_Matrix[2][1] = G_Dt * Omega_Vector[0];//x
        Update_Matrix[2][2] = 0;

        // Matrix Multiply //a*b=c
        for (int x = 0; x < 3; x++)  // rows
        {
            for (int y = 0; y < 3; y++)  // columns
            {
                //DCM_Matrix[x][y] = Update_Matrix[x][0] * Temporary_Matrix[0][y] + Update_Matrix[x][1] * Temporary_Matrix[1][y] + Update_Matrix[x][2] * Temporary_Matrix[2][y];
                Temporary_Matrix[x][y] = DCM_Matrix[x][0] * Update_Matrix[0][y] + DCM_Matrix[x][1] * Update_Matrix[1][y] + DCM_Matrix[x][2] * Update_Matrix[2][y];
            }
        }
        for (int x = 0; x < 3; x++) //Matrix Addition (update)
        {
            for (int y = 0; y < 3; y++)
            {
                DCM_Matrix[x][y] += Temporary_Matrix[x][y];
            }
        }

        float error = 0;
        Vector3[] temporary = new Vector3[3] { new Vector3(), new Vector3(), new Vector3() };

        error = -Vector3.Dot(DCM_Matrix[0], DCM_Matrix[1]) * 0.5f; //eq.19
        temporary[0] = DCM_Matrix[1] * error; //eq.19
        temporary[1] = DCM_Matrix[0] * error; //eq.19
        temporary[0] += DCM_Matrix[0]; //eq.19
        temporary[1] += DCM_Matrix[1]; //eq.19
        temporary[2] = Vector3.Cross(temporary[0], temporary[1]); // c= a x b //eq.20

        DCM_Matrix[0] = temporary[0] * (0.5f * (3 - Vector3.Dot(temporary[0], temporary[0]))); //eq.21
        DCM_Matrix[1] = temporary[1] * (0.5f * (3 - Vector3.Dot(temporary[1], temporary[1]))); //eq.21
        DCM_Matrix[2] = temporary[2] * (0.5f * (3 - Vector3.Dot(temporary[2], temporary[2]))); //eq.21

        // Drift Correction
        //float mag_heading_x;
        //float mag_heading_y;
        //float errorCourse;
        //Compensation the Roll, Pitch and Yaw drift. 
        //static float Scaled_Omega_P[3];
        float Accel_magnitude;
        float Accel_weight;


        //*****Roll and Pitch***************

        // Calculate the magnitude of the accelerometer vector
        Accel_magnitude = Convert.ToSingle(Math.Sqrt(Accel_Vector[0] * Accel_Vector[0] + Accel_Vector[1] * Accel_Vector[1] + Accel_Vector[2] * Accel_Vector[2]));

        // Dynamic weighting of accelerometer info (reliability filter)
        // Weight for accelerometer info (<0.5G = 0.0, 1G = 1.0 , >1.5G = 0.0)
        Accel_weight = (1 - 2 * Math.Abs(1 - Accel_magnitude));
        if (Accel_weight < 0)
            Accel_weight = 0;
        else if (Accel_weight > 1)
            Accel_weight = 1;
        errorRollPitch = Vector3.Cross(Accel_Vector, DCM_Matrix[2]); //adjust the ground of reference
        Omega_P = errorRollPitch * (Kp_ROLLPITCH * Accel_weight);

        Scaled_Omega_I = errorRollPitch * (Ki_ROLLPITCH * Accel_weight);
        Omega_I += Scaled_Omega_I;

        ////*****YAW***************
        //// We make the gyro YAW drift correction based on compass magnetic heading

        ////mag_heading_x = cos(MAG_Heading);
        ////mag_heading_y = sin(MAG_Heading);
        ////errorCourse = (DCM_Matrix[0][0] * mag_heading_y) - (DCM_Matrix[1][0] * mag_heading_x);  //Calculating YAW error
        ////Vector_Scale(errorYaw, &DCM_Matrix[2][0], errorCourse); //Applys the yaw correction to the XYZ rotation of the aircraft, depeding the position.

        ////Vector_Scale(&Scaled_Omega_P[0], &errorYaw[0], Kp_YAW);//.01proportional of YAW.
        ////Vector_Add(Omega_P, Omega_P, Scaled_Omega_P);//Adding  Proportional.

        ////Vector_Scale(&Scaled_Omega_I[0], &errorYaw[0], Ki_YAW);//.00001Integrator
        ////Vector_Add(Omega_I, Omega_I, Scaled_Omega_I);//adding integrator to the Omega_I

        YPR[0] = (180 / pi) * Convert.ToSingle(Math.Atan2(DCM_Matrix[1][0], DCM_Matrix[0][0])); // yaw
        YPR[1] = (180 / pi) * (-Convert.ToSingle(Math.Asin(DCM_Matrix[2][0]))); // pitch
        YPR[2] = (180 / pi) * Convert.ToSingle(Math.Atan2(DCM_Matrix[2][1], DCM_Matrix[2][2])); // roll
    }

    public Vector3 GetYPR()
    {
        return YPR;
    }
}