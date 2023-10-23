namespace SelfBalancingRobot.WebUI.Configuration;

public class MenuSettings
{
    public Menu[] MenuList { get; set; }
}
public class Menu
{
    public string code { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string url { get; set; }
    public string iconClass { get; set; }
}