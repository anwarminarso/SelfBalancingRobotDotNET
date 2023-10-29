using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SelfBalancingRobot.WebUI.Models;

namespace SelfBalancingRobot.WebUI.Pages
{
    public class SystemModel : PageModel
    {
        private readonly IMUContext imuContext;
        private readonly StabilizerContext stabilizerContext;
        private readonly ControlContext controlContext;

        public SystemModel(IMUContext imuContext, StabilizerContext stabilizerContext, ControlContext controlContext)
        {
            this.imuContext = imuContext;
            this.stabilizerContext = stabilizerContext;
            this.controlContext = controlContext;
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostResetIMUAsync()
        {
            await Task.Delay(0);

            controlContext.Deactivate();
            imuContext.StopMonitoring();
            imuContext.StartMonitoring();

            return new OkResult();
        }

        public IActionResult OnPostResetSystemAsync()
        {
            controlContext.Deactivate();
            imuContext.StopMonitoring();

            var tsk = Task.Run(() =>
            {
                Task.Delay(3000).Wait();
                Program.Shutdown();
            });
            tsk.ConfigureAwait(false);
            return new OkResult();
        }
    }
}
