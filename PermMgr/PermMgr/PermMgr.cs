using Auxide;
using System.Collections.Generic;

public class PermMgr : RustScript
{
    public PermMgr()
    {
        Author = "RFC1920";
        Description = "Permissions manager for Auxide";
        Version = new VersionNumber(1, 0, 1);
    }

    public string Lang(string input, params object[] args)
    {
        return string.Format(lang.Get(input), args);
    }

    public void Message(BasePlayer player, string input, params object[] args)
    {
        Utils.SendReply(player, string.Format(lang.Get(input), args));
    }

    public override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["added"] = "Group %1 added.",
            ["groups"] = "Groups:",
            ["notauthorized"] = "You don't have permission to do that !!"
        }, Name);
    }

    // Less than ideal to handle this in chat.  Once we get console commands working this should be integrated and cease to be a plugin at all.
    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        if (player == null) return;
        if (!player.IsAdmin)
        {
            Message(player, "notauthorized");
            return;
        }

        switch (command)
        {
            case "listgroups":
                List<string> groups = Permissions.GetGroups();
                string message = Lang("groups") + "\n";
                foreach (string group in groups)
                {
                    message += $"\t{group}\n";
                }
                Message(player, message);
                break;
            case "addgroup":
            case "groupadd":
                if (args.Length == 2)
                {
                    Permissions.AddGroup(args[1]);
                }
                break;
            case "remgroup":
            case "removegroup":
                if (args.Length == 2)
                {
                    Permissions.RemoveGroup(args[1]);
                }
                break;
            case "addtogroup":
                if (args.Length == 3)
                {
                    Permissions.AddGroupMember(args[1], args[2]);
                }
                break;
            case "removefromgroup":
            case "remfromgroup":
                if (args.Length == 3)
                {
                    Permissions.RemoveGroupMember(args[1], args[2]);
                }
                break;
            case "addperm":
            case "grantperm":
            case "grant":
                if (args.Length == 3)
                {
                    Permissions.GrantPermission(args[2], args[1]);
                }
                break;
            case "remperm":
            case "removeperm":
            case "revoke":
                if (args.Length == 3)
                {
                    Permissions.RevokePermission(args[2], args[1]);
                }
                break;
        }
    }
}
