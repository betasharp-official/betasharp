using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class WhitelistCommand : ICommand
{
    public string Usage => "whitelist <action> [player]";
    public string Description => "Manages the whitelist";
    public string[] Names => ["whitelist"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: whitelist <on|off|list|add|remove|reload> [player]");
            return;
        }

        string action = c.Args[0].ToLower();
        switch (action)
        {
            case "on":
                c.LogOp( "Turned on white-listing");
                c.Server.config.SetProperty("white-list", true);
                break;
            case "off":
                c.LogOp( "Turned off white-listing");
                c.Server.config.SetProperty("white-list", false);
                break;
            case "list":
                var whitelist = c.Server.playerManager.getWhitelist();
                string names = "";
                foreach (string name in whitelist)
                {
                    names += name + " ";
                }
                c.Output.SendMessage("White-listed players: " + names);
                break;
            case "add" when c.Args.Length >= 2:
                string addTarget = c.Args[1].ToLower();
                c.Server.playerManager.addToWhitelist(addTarget);
                c.LogOp( "Added " + addTarget + " to white-list");
                break;
            case "remove" when c.Args.Length >= 2:
                string removeTarget = c.Args[1].ToLower();
                c.Server.playerManager.removeFromWhitelist(removeTarget);
                c.LogOp( "Removed " + removeTarget + " from white-list");
                break;
            case "reload":
                c.Server.playerManager.reloadWhitelist();
                c.LogOp( "Reloaded white-list from file");
                break;
        }
    }
}
