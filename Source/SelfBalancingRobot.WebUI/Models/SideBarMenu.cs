using Iot.Device.Card.Ultralight;

namespace SelfBalancingRobot.WebUI.Models;

public class SideBarMenu
{
    public Configuration.Menu CurrentMenu { get; set; }
    public Configuration.Menu[] Menus { get; set; }
    public SideBarMenu()
    {
    }
}