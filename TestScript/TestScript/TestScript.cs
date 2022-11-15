using Auxide;

public class TestScript : RustScript
{
    public bool enable = true;
	public TestScript()
	{
        Author = "RFC1920";
        Version = new VersionNumber(1, 0, 1);
    }

    public override void Initialize()
	{
        Utils.DoLog("Let's test some hooks!");
	}

    public object CanToggleSwitch(BaseOven oven, BasePlayer player)
    {
        if (!enable) return null;
        if (oven.OwnerID != 0 && player.userID != oven.OwnerID && !Utils.IsFriend(player.userID, oven.OwnerID))
        {
            Utils.DoLog($"{player.userID} BLOCKED FROM toggling oven {oven.ShortPrefabName}");
            return true;
		}
        Utils.DoLog($"{player.userID} toggling oven {oven.ShortPrefabName}");
		return null;
    }

    public object CanToggleSwitch(ElectricSwitch sw, BasePlayer player)
    {
        if (!enable) return null;
		if (sw.OwnerID != 0 && player.userID != sw.OwnerID && !Utils.IsFriend(player.userID, sw.OwnerID))
		{
            Utils.DoLog($"{player.userID} BLOCKED FROM toggling oven {sw.ShortPrefabName}");
            return true;
		}
        Utils.DoLog($"{player.userID} toggling switch {sw.ShortPrefabName}");
		return null;
    }

    // Not yet working :(
    public object CanMount(BaseMountable entity, BasePlayer player)
    {
        if (!enable) return null;
        if (entity.OwnerID != 0 && player.userID != entity.OwnerID && !Utils.IsFriend(player.userID, entity.OwnerID))
		{
            Utils.DoLog($"{player.userID} BLOCKED FROM mounting {entity.ShortPrefabName}");
            return true;
		}
        Utils.DoLog($"{player.userID} mounting {entity.ShortPrefabName}");
		return null;
    }

    public void OnLooted(BaseEntity entity, BasePlayer player)
    {
        Utils.DoLog($"{player.userID} looting {entity.ShortPrefabName}");
    }

    public void OnPlayerJoin(BasePlayer player)
    {
        Utils.DoLog($"{player.userID} connected.");
    }

    public void OnPlayerLeave(BasePlayer player)
    {
        Utils.DoLog($"{player.userID} disconnected.");
    }

    public object OnTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
    {
        if (!enable) return null;
        Utils.DoLog($"OnTakeDamage called for {hitInfo.damageTypes.GetMajorityDamageType()}.");
        return null;
    }

    public object OnConsoleCommand(string command, bool isServer)
    {
        if (!enable) return null;
        Utils.DoLog($"OnConsoleCommand called for '{command}' with isServer={isServer}");
        if (command == "testfail") return false;
        return null;
    }

    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        string arginfo = string.Join(",", args);
        Utils.DoLog($"OnChatCommand called for '{command}' with args='{arginfo}'");
        switch (command)
        {
            case "ttoggle":
                enable = !enable;
                break;
        }
    }
}

