# HTeleport
Simple teleport plugin for Auxide

Automatically sets targets for Bandit Town and Outpost/Compound and allows for the admin to set Town.
Players can set and remove home locations for themselves.

## Commands

- `/bandit`  -- Teleports the player to the Bandit Town
- `/outpost` -- Teleports the player to the Outpost
- `/home HOME` -- Teleports the player to their saved home called HOME
- `/removehome HOME` -- Removes the player set home location called HOME
- `/sethome HOME` -- Sets a new home called HOME at the player's current location.  If a home exists with that name, it will not update it.
- `/town` -- Teleports the player to Town, if set by the admin
- `/town set` -- Sets town to the current location (for admin only)

## Configuration

```json
{
  "debug": false,
  "countdownSeconds": 5.0,
  "server": {
    "l": {
      "town": {
        "x": -1048.8396,
        "y": 21.11447,
        "z": -81.2869644
      },
      "bandit": {
        "x": -578.717651,
        "y": 34.058403,
        "z": -285.504059
      },
      "outpost": {
        "x": -87.18809,
        "y": 49.3065376,
        "z": 510.057037
      }
    },
    "t": {
      "a": 0,
      "d": null,
      "t": 0
    }
  }
}
```

- If debug is set to true, then activity will be logged to the current Auxide log in auxide/Logs.
- countdownSeconds is the global countdown until the player is telported for /bandit, /outpost, /town, and /home.
- The rest of this configuration is automatically updated.

## Datafile

The data file at auxide/Data/HTeleport/HTeleport.json will contain all player homes.
