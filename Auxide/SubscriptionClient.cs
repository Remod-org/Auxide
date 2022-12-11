using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;

namespace Auxide
{
    public class SubscriptionClient : WebClient
    {
        public AuxidePLResponse current;
        private class AuxidePLRequest
        {
            public string pluginName;
            public string action;
            //public string username;
            //public string password;
        }

        public class AuxidePLResponse
        {
            public bool success;
            public List<AuxideSubscribedPlugin> data;
        }

        public class AuxideSubscribedPlugin
        {
            public string name;
            public string raw;
            public byte[] data;
        }

        public SubscriptionClient()
        {
            if (Auxide.config.Options.subscription.enabled)
            {
                try
                {
                    _httpClient = new HttpClient() { BaseAddress = new Uri("https://code.remod.org/") };
                    _username = Auxide.config.Options.subscription.username;

                    if (!string.IsNullOrEmpty(Auxide.config.Options.subscription.password))
                    {
                        // This all works as desired...
                        Auxide.config.Options.subscription.encrypted = EncryptString(Auxide.config.Options.subscription.password);
                        Auxide.config.Options.subscription.password = "";
                        Auxide.config.Save();
                    }
                    _password = Auxide.config.Options.subscription.encrypted;

                    AuxideSub();
                }
                catch
                {
                    Utils.DoLog("Unable to process subscription data.  Possible missing config values for username/password?");
                }
            }
        }

        public async void AuxideSub()
        {
            AuxidePLRequest request = new AuxidePLRequest()
            {
                action = "getsubs",
                pluginName = null
            };

            AuxidePLResponse response = await PostAsync<AuxidePLResponse>("/auxide", request);
            if (response != null && response.success)
            {
                current = new AuxidePLResponse();

                foreach (AuxideSubscribedPlugin plugin in response.data)
                {
                    current.data.Add(new AuxideSubscribedPlugin
                    {
                        name = plugin.name,
                        data = Convert.FromBase64String(plugin.raw)
                    });
                }
            }
        }

        // https://medium.com/@mehanix/lets-talk-security-salted-password-hashing-in-c-5460be5c3aae
        public static byte[] GenSalt()
        {
            // Create new salt for hashing passwords
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            return salt;
            //return Encoding.ASCII.GetString(salt);
        }

        //public static bool Authenticate(string username, string passwd)
        //{
        //    string crypted = Auxide.config.Options.subscription.password;

        //    if (crypted != "")
        //    {
        //        byte[] hashBytes = Convert.FromBase64String(crypted);
        //        byte[] salt = new byte[16];
        //        Array.Copy(hashBytes, 0, salt, 0, 16);
        //        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(passwd, salt, 10000);
        //        byte[] hash = pbkdf2.GetBytes(20);

        //        int ok = 1;
        //        for (int i = 0; i < 20; i++)
        //        {
        //            if (hashBytes[i + 16] != hash[i])
        //            {
        //                ok = 0;
        //            }
        //        }
        //        if (ok == 1)
        //        {
        //            Utils.DoLog($"User {username} successfully authenticated!");
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public static string EncryptString(string passwd, byte[] insalt = null)
        {
            byte[] salt = insalt ?? GenSalt();
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(passwd, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];

            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }
    }
}
