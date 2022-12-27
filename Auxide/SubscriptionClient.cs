using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
            Task.Run(CheckSubs);
        }

        public async Task CheckSubs()
        {
            if (Auxide.config.Options.subscription.enabled)
            {
                try
                {
                    _httpClient = new HttpClient() { BaseAddress = new Uri("https://code.remod.org/") };
                    _username = Auxide.config.Options.subscription.username;

                    //if (!string.IsNullOrEmpty(Auxide.config.Options.subscription.password))
                    //{
                    //    // This all works as desired...
                    //    Auxide.config.Options.subscription.encrypted = EncryptString(Auxide.config.Options.subscription.password);
                    //    Auxide.config.Options.subscription.password = "";
                    //    Auxide.config.Save();
                    //}
                    //_password = Auxide.config.Options.subscription.encrypted;
                    _password = Auxide.config.Options.subscription.password;

                    await AuxideSub();
                }
                catch
                {
                    Utils.DoLog("Unable to process subscription data.  Possible missing config values for username/password?");
                }
            }
        }

        public async Task AuxideSub()
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

            //Auxide.config.Options.subscription.salt = Convert.ToBase64String(salt);
            //Auxide.config.Save();

            return salt;
        }

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

        public string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(salt);

            // Concatenate the password and salt
            byte[] passwordAndSalt = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();

            // Create a SHA-256 hash object
            SHA256 sha256 = SHA256.Create();

            // Compute the hash of the password and salt
            byte[] passwordHash = sha256.ComputeHash(passwordAndSalt);

            // Convert the hash to a hexadecimal string
            string passwordHashString = BitConverter.ToString(passwordHash).Replace("-", "");

            // Return the password hash
            return passwordHashString;
        }
    }
}
