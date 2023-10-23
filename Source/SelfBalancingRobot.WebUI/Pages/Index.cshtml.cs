using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SelfBalancingRobot.WebUI.Models;

namespace SelfBalancingRobot.WebUI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IMUContext imuContext;
        public IndexModel(ILogger<IndexModel> logger, IMUContext imuContext)
        {
            _logger = logger;
            this.imuContext = imuContext;
        }

        public void OnGet()
        {

        }
    }
}