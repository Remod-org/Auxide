### Overview
Auxide is a Harmony patch dll for Rust which provides a simplified alternative to Oxide/uMod/Carbon.  It is not a compatible replacement for them and should be used on its own.  Many of the features of those other systems are available here.  But, the primary goal is to provide a way for vanilla and staging servers to have more control than what they get out of the box without having to install a larger package.

It offers the admin two modes of operation:

- MINIMAL
  - Provides basic controls for PVE, decay management, and access control.

- FULL (Work in progress)
  - Provides full plugin support (Auxide plugins only)
  - Plugins are currently required to be in DLL form, and several examples are provided.

Additionally, Auxide should work just fine month to month for **vanilla and staging** servers without monthly updates.

### NOTES

- This was originally a fork of https://github.com/Facepunch/Rust.ModLoader.
- This should work on either Windows or Linux Rust servers.
- Download of the production releases (when ready) will be at https://remod.org.
- As of November 2022, this has been in silent development long enough.
- If following things here at least in the pre-release versions, verify that your Auxide.json is up to date with new or changed configuration items.  Otherwise, the dll may fail to load.
- PVP damage prevention still being tested.
- Please let me know via Discord https://discord.gg/2z736h7sMt if you encounter any major issues.
- If you have something to contribute, changes you'd like to see, etc., please provide patches or pull requests, etc.  I would rather see this improved here rather than forked for the sake of forking.

### Folders

The following folders are created in both modes.  Only the Logs folder is used in minimal mode:

- TOPLEVEL
  - auxide
    - Bin (temporary download location for the compiler when using external, currently unused and will be temporarily removed)
    - Config (plugin config files)
    - Data (plugin data files including automatic creation of subfolders for each plugin)
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

We have included some mostly working plugins in the source code as examples for improvement, etc.

As of 15 November 2022, permissions are working.  See the file Permissions.md for more information.

#### Hooks in Full Mode

Below, the term Narrowcast essentially means target, as in not sent to all scripts, just one.

```cs
void Narrowcast("OnScriptLoaded", RustScript);

void Narrowcast("LoadData", RustScript);

void Narrowcast("LoadConfig", RustScript);

void Narrowcast("LoadDefaultMessages", RustScript);

void Broadcast("OnPluginLoaded", RustScript);

void Broadcast("OnServerInitialized");

void Broadcast("OnServerShutdown");

void Broadcast("OnServerSave");

void Broadcast("OnNewSave");

object BroadcastReturn("CanUseUI", BasePlayer, string);

void Broadcast("OnDestroyUI", BasePlayer, string);

object BroadcastReturn("CanAdminTC", BuildingPrivlidge, BasePlayer);

object BroadcastReturn("CanToggleSwitch", oven, BasePlayer);

object BroadcastReturn("CanToggleSwitch", sw, BasePlayer);

void Broadcast("OnToggleSwitch", oven, BasePlayer);

void Broadcast("OnToggleSwitch", sw, BasePlayer);

object BroadcastReturn("CanMount", BaseMountable, BasePlayer);

void Broadcast("OnMounted", BaseMountable, BasePlayer);

object BroadcastReturn("CanLoot", BaseEntity, BasePlayer, string);

void Broadcast("OnLooted", BaseEntity, BasePlayer);

object BroadcastReturn("CanPickup", BaseEntity, BasePlayer);

object BroadcastReturn("CanPickup", ContainerIOEntity, BasePlayer);

object BroadcastReturn("CanPickup", StorageContainer, BasePlayer);

object BroadcastReturn("OnTakeDamage", BaseCombatEntity, HitInfo);

void Broadcast("OnPlayerJoin", BasePlayer);

void Broadcast("OnPlayerLeave", BasePlayer);

void Broadcast("OnChatCommand", BasePlayer, string, object[]);
```

### Available Utilities and Classes for Plugins

The following were borrowed from Oxide, and I believe their licensing allows this:
- DynamicConfigFile (as config)
- DataFileSystem (as data)
- CuiHelper (Auxide.CuiHelper)
  - Currently unusable due to non-working console commands (Displays correctly but cannot close, etc.).
  - k1lly0u's (?) UI class is included as Auxide.UI.

- LangFileSystem (as lang)
  - See plugin examples for helper functions Lang() and Message()

### TODO

- Fix plugin updating, which in one case can crash the server.  The other case is a failure to detect the included plugin class when loading a new version.
- Revisit cs file compilation
- Fix hideGiveNotices
- I am sure there are issues I have missed.


### CREDITS, ETC.

Many thanks to SureL0ck for testing and patience.

