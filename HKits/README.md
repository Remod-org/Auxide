# HKits
Basic kits plugin for Auxide full mode

## Commands

- `/kit NAME` -- Issues the kit with the associated NAME to the player
- `/kit list` -- Lists available kits
- `/kit create NAME` -- For the admin, will create a kit based on their current inventory\
- `/kits` -- EXPERIMENTAL - Brings up a GUI (work in progress) for kit selection including kit descriptions.  More to come with this one.  It may not close properly at this point until the player logs off.

## Configuration

So far, not much:

```json
{
  "debug": false
}
```

## Data file

The data file at auxide/Data/HKits/kits.json will contain all admin-created kit information.
