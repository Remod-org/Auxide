# Permissions

Currently, all permissions manipulation including group management is done either via chat or directly in sqlite.  Here, we will only document the commands needed to manage things via chat.

## NOTES

Permissions includes plugin permissions as well as group management.  Groups may be members of other groups.  Of course, to ultimately be useful, players must be members of one or more groups.

By default, we create the groups default and admin.  Anyone with admin rights on the server will be added to the admin group.  ALL players are added to the default group.

Work is in progress to ensure uniqueness of the stored info, i.e. use of user id / steam id instead of name in the actual database.

No feedback is currently given in response to these commands.  Look for expansion to include listing group members, etc.

## Commands

- `/grant USERGROUP PERMISSION` -- Grant a user or group the named permission
- `/addperm USERGROUP PERMISSION` -- Alias for the above.
- `/grantperm USERGROUP PERMISSION` -- Alias for the above.

- `/revoke USERGROUP PERMISSION` -- Revoke the named permission from a user or group
- `/remperm USERGROUP PERMISSION` -- Alias for the above.
- `/removeperm USERGROUP PERMISSION` -- Alias for the above.

- `listgroups` -- List groups.  Default groups are admin and default.  A player with admin rights will be added to the admin group on connect, and all players will be added to the default group.

- `addgroup GROUPNAME` -- Adds a new group called GROUPNAME.
- `groupadd GROUPNAME` -- Alias for the above.

- `remgroup GROUPNAME` -- Removes the group named GROUPNAME

- `removegroup GROUPNAME` -- Alias for the above.

- `addtogroup GROUPNAME USERGROUP` -- Adds the named user or group to the group called GROUPNAME

- `remfromgroup GROUPNAME USERGROUP` -- Removes the named user or group from the group called GROUPNAME
- `removefromgroup GROUPNAME USERGROUP` -- Alias for the above.

### Example 1

1. Create a new group called banker using /addgroup banker
2. Add player Bob to the group using /addtogroup banker Bob

### Example 2

This example sets up permissions for an imaginary Genius plugin.  Bob and Dave can both use the plugin, but each has different rights or specializations.

1. Create a new group called geniuses
- /addgroup geniuses.

2. Create new groups that are members of that group using:
- /addgroup salesgeniuses
- /addgroup towngeniuses
- /addtogroup geniuses salesgeniuses
- /addtogroup geniuses towngeniuses

3. Add Bob to salegeniuses and Dave to towngeniuses
- /addtogroup salesgeniuses Bob
- /addtogroup towngeniuses Dave

4. Add permissions to each group
- /grant geniuses genius.use
- /grant salesgeniuses genius.sales
- /grant towngeniuses genius.town

The above sets up something like the following:

GROUPNAME, PERMISSION, MEMBERS

geniuses, genius.use, Bob AND Dave

salesgenius, genius.sales, Bob
  
towngenius, genius.town, Dave
