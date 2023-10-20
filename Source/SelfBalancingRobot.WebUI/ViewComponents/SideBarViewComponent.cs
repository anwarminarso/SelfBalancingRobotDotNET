using Microsoft.AspNetCore.Mvc;
using SelfBalancingRobot.WebUI.Configuration;
using SelfBalancingRobot.WebUI.Models;
using System;
using System.Linq;

namespace SelfBalancingRobot.WebUI
{
    public class SideBarViewComponent : ViewComponent
    {
        private readonly MenuSettings menuSettings;
        public SideBarViewComponent(MenuSettings menuSettings)
        {
            this.menuSettings = menuSettings ?? throw new ArgumentNullException(nameof(menuSettings));
        }
        public IViewComponentResult Invoke()
        {
            string code = string.Empty;
            if (ViewData["code"] != null)
                code = ViewData["code"].ToString();
            var mn = new SideBarMenu();
            mn.Menus = menuSettings.MenuList;
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            mn.CurrentMenu = mn.Menus.Where(t => t.code == code).FirstOrDefault();
            return View(mn);
        }
    }
}
