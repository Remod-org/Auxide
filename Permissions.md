# Permissions

Currently, all permissions manipulation including group management is done either via chat or directly in sqlite.  Here, we will only document the commands needed to manage things via chat.

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

