using System;
using System.IO;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Auxide
{
    public sealed class Permissions : Auxide
    {
        // Handles Auxide groups as well as permissions for users and groups.
        // Groups can be nested.
        private static string connStr;

        public Permissions()
        {
            connStr = $"Data Source={Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide_permissions.db")};";
            bool exists = false;
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand r = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'permissions'", c))
                {
                    using (SqliteDataReader rentry = r.ExecuteReader())
                    {
                        while (rentry.Read())
                        {
                            exists = true;
                            break;
                        }
                    }
                }
            }
            if (!exists) DefaultDatabase();
        }

        private void DefaultDatabase()
        {
            Utils.DoLog("Loading default database");
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand(c))
                using (SqliteTransaction transaction = c.BeginTransaction())
                {
                    cmd.CommandText += $"CREATE TABLE permissions (plugin varchar(32), source int(1) DEFAULT 0, permname varchar(32), userid varchar(32), isgroup int(1) DEFAULT 0);";
                    cmd.CommandText += $"CREATE TABLE groups (groupname varchar(32), members varchar(256));";
                    cmd.CommandText += $"INSERT INTO groups VALUES('default', '');";
                    cmd.CommandText += $"INSERT INTO groups VALUES('admin', '');";
                    Utils.DoLog(cmd.CommandText);
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
        }

        public static void RegisterPermission(string plugin, string permname)
        {
            bool exists = false;
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand r = new SqliteCommand($"SELECT * FROM permissions WHERE plugin='{plugin}' AND source=1 AND permname='{permname}' AND isgroup=0", c))
                {
                    using (SqliteDataReader rentry = r.ExecuteReader())
                    {
                        while (rentry.Read())
                        {
                            exists = true;
                            break;
                        }
                    }
                }
            }
            if (exists) return;

            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                string query = $"INSERT INTO permissions VALUES('{plugin}', 1, '{permname}', '', 0);";
                if (verbose) Utils.DoLog(query);
                using (SqliteCommand cmd = new SqliteCommand(query, c))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void GrantPermission(string permname, string usergroup)
        {
            if (BasePlayer.Find(usergroup) != null)
            {
                _GrantPermission(permname, usergroup, 0);
                return;
            }
            _GrantPermission(permname, usergroup, 1);
        }

        private static void _GrantPermission(string permname, string usergroup, int isgroup)
        {
            if (UserHasPermission(permname, usergroup)) return;
            string plugin = null;
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT plugin FROM permissions WHERE permname='{permname}' AND source=1", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            plugin = rdr.GetString(0);
                        }
                    }
                }
            }
            if (plugin != null)
            {
                using (SqliteConnection c = new SqliteConnection(connStr))
                {
                    c.Open();
                    string query = $"INSERT INTO permissions VALUES('{plugin}', 0, '{permname}', '{usergroup}', {isgroup});";
                    if (verbose) Utils.DoLog(query);
                    using (SqliteCommand cmd = new SqliteCommand(query, c))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        public static void RevokePermission(string permname, string usergroup)
        {
            if (BasePlayer.Find(usergroup) != null)
            {
                _RevokePermission(permname, usergroup, 0);
                return;
            }
            _RevokePermission(permname, usergroup, 1);
        }

        private static void _RevokePermission(string permname, string usergroup, int isgroup)
        {
            string plugin = null;
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT plugin FROM permissions WHERE permname='{permname}' AND source=1", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            plugin = rdr.GetString(0);
                        }
                    }
                }
            }
            if (plugin != null)
            {
                using (SqliteConnection c = new SqliteConnection(connStr))
                {
                    c.Open();
                    using (SqliteCommand cmd = new SqliteCommand(c))
                    using (SqliteTransaction transaction = c.BeginTransaction())
                    {
                        cmd.CommandText += $"DELETE FROM permissions WHERE plugin='{plugin}' AND source=0, AND permname='{permname}' AND userid='{usergroup}' AND isgroup={isgroup});";
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
            }
        }

        public static bool UserHasPermission(string permname, string userid)
        {
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT plugin FROM permissions WHERE permname='{permname}' AND (userid='{userid}' AND isgroup=0)", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            // User has direct permission.
                            return true;
                        }
                    }
                }
            }
            if (GetUserGroupsWithPermission(userid, permname).Count > 0)
            {
                // User is in group with permission.
                return true;
            }
            return false;
        }

        public static List<string> GetUserGroupsWithPermission(string userid, string permname)
        {
            List<string> res = new List<string>();
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT groupname FROM groups WHERE members LIKE '%{userid}%'", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string group = rdr.GetString(0);
                            using (SqliteConnection s = new SqliteConnection(connStr))
                            {
                                s.Open();
                                using (SqliteCommand subcmd = new SqliteCommand($"SELECT plugin FROM permissions WHERE permname='{permname}' AND (userid='{group}' AND isgroup=1)", c))
                                {
                                    using (SqliteDataReader subrdr = subcmd.ExecuteReader())
                                    {
                                        while (subrdr.Read())
                                        {
                                            res.Add(subrdr.GetString(0));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return res;
        }

        public static List<string> GetUserGroups(string userid)
        {
            List<string> res = new List<string>();
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT groupname FROM groups WHERE members LIKE '%{userid}%'", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            res.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            if (!res.Contains("default")) res.Add("default");
            return res;
        }

        public static void AddGroup(string groupname)
        {
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT members FROM groups WHERE groupname='{groupname}'", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            return;
                        }
                    }
                }
            }
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                string query = $"INSERT INTO groups VALUES('{groupname}', '');";
                if (verbose) Utils.DoLog(query);
                using (SqliteCommand cmd = new SqliteCommand(query, c))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            //Scripts.OnGroupCreatedHook(groupname, groupname, 0);
        }

        public static void RemoveGroup(string groupname)
        {
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand(c))
                using (SqliteTransaction transaction = c.BeginTransaction())
                {
                    cmd.CommandText += $"DELETE FROM groups WHERE groupname='{groupname}';";
                    cmd.CommandText += $"DELETE FROM permissions WHERE userid='{groupname}' AND isgroup=1";
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
        }

        public static void AddGroupMember(string groupname, string usergroup)
        {
            if (groupname == usergroup) return;
            string members = null;
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT members FROM groups WHERE groupname='{groupname}'", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            members = rdr.GetString(0);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(members))
            {
                members = usergroup;
            }
            else
            {
                List<string> memberList = members.Split(',').ToList();
                if (memberList.Contains(usergroup)) return;

                memberList.Add(usergroup);
                members = string.Join(",", memberList.ToArray());
            }
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand(c))
                using (SqliteTransaction transaction = c.BeginTransaction())
                {
                    string query = $"UPDATE groups SET members='{members}' WHERE groupname='{groupname}';";
                    if (verbose) Utils.DoLog(query);
                    cmd.CommandText += query;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
        }

        public static void RemoveGroupMember(string groupname, string usergroup)
        {
            if (groupname == usergroup) return;
            string members = null;
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT members FROM groups WHERE groupname='{groupname}'", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            members = rdr.GetString(0);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(members))
            {
                return;
            }
            else
            {
                List<string> memberList = members.Split(',').ToList();
                if (!memberList.Contains(usergroup)) return;

                memberList.Remove(usergroup);
                members = string.Join(",", memberList.ToArray());
            }
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand(c))
                using (SqliteTransaction transaction = c.BeginTransaction())
                {
                    string query = $"UPDATE groups SET members='{members}' WHERE groupname='{groupname}';";
                    if (verbose) Utils.DoLog(query);
                    cmd.CommandText += query;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
        }

        public static List<string> GetGroups()
        {
            List<string> res = new List<string>();
            using (SqliteConnection c = new SqliteConnection(connStr))
            {
                c.Open();
                using (SqliteCommand cmd = new SqliteCommand($"SELECT groupname FROM groups", c))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            res.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            return res;
        }

        public enum Roles
        {
            admin = 0,
            moderator = 1,
            player = 2
        }
    }
}
