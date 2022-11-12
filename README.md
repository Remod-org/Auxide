### Overview
Auxide is a Harmony patch dll for Rust which provides a simplified alternative to Oxide/uMod/Carbon.  It is not a compatible replacement for them and should be used on its own.

It offers the admin with two modes of operation:

- MINIMAL
  - Provides basic controls for PVE.

- FULL (Work in progress)
  - Provides full plugin support (Auxide plugins only)
  - Plugins are currently required to be in DLL form, and 3 examples are provided.

Additionally, Auxide should work just fine month to month for **vanilla and staging** servers without monthly updates.

### NOTES

- This was originally a fork of https://github.com/Facepunch/Rust.ModLoader.
- This should work on either Windows or Linux Rust servers.
- Download of the dll (when ready) will be at https://remod.org.
- As of November 2022, this has been in silent development long enough.
- I am sure there are issues I have missed.
- hideGiveNotices not yet working.
- If following things here at least in the pre-release versions, verify that your Auxide.json is up to date with new or changed configuration items.  Otherwise, the dll may fail to load.
- PVP damage prevention still being tested.
- Please let me know via Discord https://discord.gg/2z736h7sMt if you encounter any major issues.
- If you have something to contribute, changes you'd like to see, etc., please provide patches or pull requests, etc.  I would rather see this improved here rather than forked for the sake of forking.

### Folders

The following folders are created in both modes.  Only the Logs folder is used in minimal mode:

- TOPLEVEL
  - auxide
    - Bin (temporary download location for the compiler when using external)
    - Config (plugin config files)
    - Data (plugin data files)
    - Logs (Auxide logging, especially in verbose mode)
    - Scripts (plugins)

### Minimal (working)

The original goal of Auxide was to provide an alternative means of PVE access management for users of **vanilla and staging (yes, that's right)** servers.  In minimal mode, it handles this by patching the standard calls for damage, decay, loot, and mounting access.  It will also allow the admin to disable the TC decay warning for either mode.

Configuration is handled via Auxide.json, also contained in the HarmonyMods folder.  In this example, minimal mode would be used, and the minimal section will be used. useInternalCompiler will be ignored:

```json
{
	"Options": {
		"full": false,
		"verbose": true,
		"disableTCWarning": true,
		"hideGiveNotices": false,
		"minimal": {
			"blockTCMenu": true,
			"allowPVP": false,
			"allowAdminPVP": true,
			"blockBuildingDecay": true,
			"blockDeployablesDecay": true,
			"protectLoot": true,
			"protectMount":true,
			"allowNPCDamage": true
		}
	}
}
```

The configuration is re-read on server save in case you want to make adjustments during runtime.

Access control is managed by checking the playerid against the ownerid for an object.  It also by default checks for team members using the built-in Rust team functionality to also allow team member access.

### Full (Work in progress)

Full mode intends to enable the same vanilla and staging servers to also use plugins customized for use with Auxide to allow for extended functions such as teleport, item spawning, etc.  This is still a work in progress.  But, the included examples in dll form do actually work.  For now, we have opted to NOT support C# compilation on the fly.  This is now working as of 11/11/2022, and we offer several internal hook calls for these plugins to use as with other modding platforms.

**THIS IS NOT COMPATIBLE WITH ANY EXISTING PLUGIN FOR RUST**, whether used by Oxide, uMod, or Carbon, or derivatives.

Configuration is handled via Auxide.json, also contained in the HarmonyMods folder.  In this example, full mode would be used, and the minimal section will be ignored:

```json
{
	"Options": {
		"full": true,
		"verbose": true,
		"disableTCWarning": true,
		"hideGiveNotices": false,
		"minimal": {
			"blockTCMenu": true,
			"allowPVP": false,
			"allowAdminPVP": true,
			"blockBuildingDecay": true,
			"blockDeployablesDecay": true,
			"protectLoot": true,
			"protectMount":true,
			"allowNPCDamage": true
		}
	}
}
```

The configuration is re-read on server save in case you want to make adjustments during runtime.

#### Hooks in Full Mode

```cs
void Broadcast("OnServerInitialized");

void Broadcast("OnServerShutdown");

void Broadcast("OnServerSave");

void Broadcast("OnGroupCreated", group, title, rank);

void Broadcast("OnUserGroupAdded", id, name);

object BroadcastReturn("CanAdminTC", bp, player);

object BroadcastReturn("CanToggleSwitch", oven, player);

object BroadcastReturn("CanToggleSwitch", sw, player);

void Broadcast("OnToggleSwitch", oven, player);

void Broadcast("OnToggleSwitch", sw, player);

object BroadcastReturn("CanMount", entity, player);

void Broadcast("OnMounted", entity, player);

object BroadcastReturn("CanLoot", entity, player, panelName);

void Broadcast("OnLooted", entity, player);

object BroadcastReturn("CanPickup", entity, player);

object BroadcastReturn("CanPickup", entity, player);

object BroadcastReturn("CanPickup", entity, player);

object BroadcastReturn("OnTakeDamage", target, info);

void Broadcast("OnPlayerJoin", player);

void Broadcast("OnPlayerLeave", player);

void Broadcast("OnChatCommand", player, chat, args);
```

### CREDITS, ETC.

More about plugins, etc., when the compilation is working consistently.

Many thanks to SureL0ck for testing and patience.

