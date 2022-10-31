Auxide is a Harmony patch dll for Rust which provides a simplified alternative to Oxide/uMod/Carbon.  It offers the admin with two modes of operation:

- MINIMAL
  - Provides basic controls for PVE.

- FULL (Work in progress)
  - Provides full plugin support (Auxide plugins only)

Additionally, Auxide should work just fine month to month for vanilla and staging servers without monthly updates.

### Minimal

The original goal of Auxide was to provide an alternative means of PVE access management for users of vanilla and staging (yes, that right) servers.  In minimal mode, it handles this by patching the standard calls for damage, decay, loot, and mounting access.  It will also allow the admin to disable the TC decay warning for either mode.

Configuration is handled via Auxide.json, also contained in the HarmonyMods folder.  In this example, minimal mode would be used, and the minimal section will be used. useInternalCompiler will be ignored:

```json
{
	"Options": {
		"full": false,
			"verbose": true,
			"useInternalCompiler": true,
			"disableTCWarning": true,
			"minimal": {
				"blockTCMenu": true,
				"allowPVP": false,
				"allowAdminPVP": true,
				"blockBuildingDecay": true,
				"blockDeployablesDecay": true,
				"protectLoot": true,
				"protectMount":true
			}
	}
}
```

Access control is managed by checking the playerid against the ownerid for an object.  It also by default checks for team members using the built-in Rust team functionality to also allow team member access.
Full (Work in progress)

Full mode intends to enable the same vanilla and staging servers to also use plugins customized for use with Auxide to allow for extended functions such as teleport, item spawning, etc.  This is still a work in progress primarily due to major issues trying to get our code compiler to work in a non-hackish and consistent way.  Barring that major issue, it has been shown to work, offering several internal hook calls for these plugins to use as with other modding platforms.

Configuration is handled via Auxide.json, also contained in the HarmonyMods folder.  In this example, full mode would be used, and the minimal section will be ignored:

```json
{
	"Options": {
		"full": true,
			"verbose": true,
			"useInternalCompiler": true,
			"disableTCWarning": true,
			"minimal": {
				"blockTCMenu": true,
				"allowPVP": false,
				"allowAdminPVP": true,
				"blockBuildingDecay": true,
				"blockDeployablesDecay": true,
				"protectLoot": true,
				"protectMount":true
			}
	}
}
```

Hooks in Full Mode

```cs
Broadcast("OnServerInitialized");

Broadcast("OnServerShutdown");

Broadcast("OnServerSave");

Broadcast("OnGroupCreated", group, title, rank);

Broadcast("OnUserGroupAdded", id, name);

return BroadcastReturn("CanAdminTC", bp, player);

return BroadcastReturn("CanToggleSwitch", oven, player);

return BroadcastReturn("CanToggleSwitch", sw, player);

Broadcast("OnToggleSwitch", oven, player);

Broadcast("OnToggleSwitch", sw, player);

return BroadcastReturn("CanMount", entity, player);

Broadcast("OnMounted", entity, player);

return BroadcastReturn("CanLoot", entity, player, panelName);

Broadcast("OnLooted", entity, player);

return BroadcastReturn("CanPickup", entity, player);

return BroadcastReturn("CanPickup", entity, player);

return BroadcastReturn("CanPickup", entity, player);

return BroadcastReturn("OnTakeDamage", target, info);

Broadcast("OnPlayerJoin", player);

Broadcast("OnPlayerLeave", player);

Broadcast("OnChatCommand", player, chat, args);
```
