using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SelfBalancingRobot.WebUI.Configuration;
using SelfBalancingRobot.WebUI.Models;
using System.Numerics;
using System.Security.Cryptography;

namespace SelfBalancingRobot.WebUI.Pages
{
    public class CalibrationModel : PageModel
    {
        private readonly CalibrationSettings calibrationSettings;
        private readonly WritableConfiguration writableConfiguration;
        private readonly IMUContext imuContext;
        public PIDSetting YawPID
        {
            get { return calibrationSettings.YawPID; }
        }
        public PIDSetting PitchPID
        {
            get { return calibrationSettings.PitchPID; }
        }
        public PIDSetting AnglePID
        {
            get { return calibrationSettings.AnglePID; }
        }
        public OffsetSettings GyroOffsets
        {
            get { return calibrationSettings.GyroOffsets; }
        }
        public OffsetSettings AccelOffset
        {
            get { return calibrationSettings.AccelOffsets; }
        }
        public CalibrationModel(CalibrationSettings calibrationSettings, WritableConfiguration writableConfiguration, IMUContext imuContext)
        {
            this.calibrationSettings = calibrationSettings;
            this.writableConfiguration = writableConfiguration;
            this.imuContext = imuContext;
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostSavePID(string jsonPID)
        {
            try
            {
                var pid = JsonConvert.DeserializeObject<PIDHelper>(jsonPID);
                await writableConfiguration.UpdateAsync<CalibrationSettings>((x) =>
                {
                    calibrationSettings.YawPID = pid.Yaw;
                    calibrationSettings.PitchPID = pid.Pitch;
                    calibrationSettings.AnglePID = pid.Angle;
                    x.YawPID = pid.Yaw;
                    x.PitchPID = pid.Pitch;
                    x.AnglePID = pid.Angle;
                }, Utils.ConfigName_CalibrationSettings);
                return new OkObjectResult(pid);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
        public async Task<IActionResult> OnPostSaveGyroOffsetAsync(string jsonOffsets)
        {
            try
            {
                var offsets = JsonConvert.DeserializeObject<GyroAccHelper>(jsonOffsets);
                imuContext.StopMonitoring();
                await writableConfiguration.UpdateAsync<CalibrationSettings>((x) =>
                {
                    calibrationSettings.GyroOffsets = new OffsetSettings(offsets.Offsets);
                    x.GyroOffsets = new OffsetSettings(offsets.Offsets);
                }, Utils.ConfigName_CalibrationSettings);
                imuContext.StartMonitoring();
                return new OkObjectResult(new
                {
                    Offsets = new float[] { calibrationSettings.GyroOffsets.X, calibrationSettings.GyroOffsets.Y, calibrationSettings.GyroOffsets.Z }
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
        public async Task<IActionResult> OnPostSaveAccOffsetAsync(string jsonOffsets)
        {
            try
            {
                var offsets = JsonConvert.DeserializeObject<GyroAccHelper>(jsonOffsets);
                imuContext.StopMonitoring();
                await writableConfiguration.UpdateAsync<CalibrationSettings>((x) =>
                {
                    calibrationSettings.AccelOffsets = new OffsetSettings(offsets.Offsets);
                    x.AccelOffsets = new OffsetSettings(offsets.Offsets);
                }, Utils.ConfigName_CalibrationSettings);
                imuContext.StartMonitoring();
                return new OkObjectResult(new
                {
                    Offsets = new float[] { calibrationSettings.AccelOffsets.X, calibrationSettings.AccelOffsets.Y, calibrationSettings.AccelOffsets.Z }
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
        public async Task<IActionResult> OnPostHardwareCalibrationAsync(string jsonOffsets)
        {
            try
            {
                await Task.Delay(0);
                imuContext.HardwareCalibrate();
                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private class PIDHelper
        {
            public PIDSetting Yaw { get; set; }
            public PIDSetting Pitch { get; set; }
            public PIDSetting Angle { get; set; }
        }
        private class GyroAccHelper
        {
            public float[] Offsets { get; set; }
        }
    }
}
