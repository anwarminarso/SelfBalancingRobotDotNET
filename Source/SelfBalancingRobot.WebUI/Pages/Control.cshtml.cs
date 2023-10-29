using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using SelfBalancingRobot.WebUI.Configuration;
using SelfBalancingRobot.WebUI.Models;

namespace SelfBalancingRobot.WebUI.Pages
{
    public class ControlModel : PageModel
    {
        private readonly ControlContext controlContext;

        public bool JoystickEnabled { get {  return controlContext.JoystickEnabled; } }
        public bool Armed { get {  return controlContext.Armed; } }
        public ControlModel(ControlContext controlContext)
        {
            this.controlContext = controlContext;
        }
        public void OnGet()
        {
        }
    }
}
