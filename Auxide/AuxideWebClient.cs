using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Auxide
{
    internal sealed class AuxideWebClient
    {
        public string url = "https://code.remod.org/auxide";
        //public static string hostname;
        public static string clientname;
        public static int port;
        public string ServerMessage;
        public PlResponse response;
        //public byte[] PluginData;
        public static bool validate;

        public class PlRequest
        {
            public string pluginName;
            public string action;
            public string username;
            public string password;
        }

        public class PlRaw
        {
            public bool success;
            public Dictionary<string, string> data;
        }

        public class PlResponse
        {
            public bool success;
            public Dictionary<string, byte[]> data;
        }

        public async void GetSubscriptionPlugins(Dictionary<string, PlRequest> plr)
        {
            //string msg = JsonConvert.SerializeObject(plr);
            foreach (KeyValuePair<string, PlRequest> plugin in plr)
            {
                await RunClient(url, port, plugin.Value).ConfigureAwait(false);
                try
                {
                    response = ParseServerMessage();
                    //if (response.success)
                    //{
                    //    foreach(KeyValuePair<string, byte[]> plugin in response.data)
                    //    {
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    Utils.DoLog($"Failed to parse server response: {e}");
                }
            }
        }

        public AuxideWebClient(string cname)
        {
            Client(cname, url, 443);
        }

        private void Client(string cname, string addr, int pt = 443, bool dovalidate = true)
        {
            Utils.DoLog($"Setting up client to {addr}:{pt}");
            clientname = cname;
            url = addr;
            port = pt;
            validate = dovalidate;
        }

        //public async void Main(string message)
        //{
        //    await RunClient(hostname, port, message).ConfigureAwait(false);
        //}

        public async Task RunClient(string serverName, int port, PlRequest plr)
        {
            // Create a TCP/IP client socket.
            // machineName is the host running the server application.
            //HttpClient client = new HttpClient(serverName, port);
            HttpClient client = new HttpClient();
            byte[] authToken = Encoding.ASCII.GetBytes($"{plr.username}:{plr.password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
            Utils.DoLog("Client connected.");
            string json = JsonConvert.SerializeObject(plr);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage result;

            try
            {
                //await sslStream.AuthenticateAsClientAsync(serverName);
                //await sslStream.AuthenticateAsClientAsync(serverName, null, SslProtocols.Tls13, false);
                result = await client.PostAsync(serverName, data);
            }
            catch (AuthenticationException e)
            {
                Utils.DoLog(string.Format("Exception: {0}", e.Message));
                if (e.InnerException != null)
                {
                    Utils.DoLog(string.Format("Inner exception: {0}", e.InnerException.Message));
                }
                Utils.DoLog("Authentication failed - closing the connection.");
                client.Dispose();
                return;
            }
        }

        private async Task ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            int chunkSize = 8000000;
            byte[] buffer = new byte[chunkSize];
            StringBuilder messageData = new StringBuilder();

            int bytes;
            do
            {
                bytes = await sslStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
            }
            while (bytes != 0);

            ServerMessage = messageData.ToString();
        }

        private PlResponse ParseServerMessage()
        {
            PlRaw resp = (PlRaw)JsonConvert.DeserializeObject(ServerMessage, typeof(PlRaw));
            response.success = resp.success;
            foreach (KeyValuePair<string, string> item in resp.data)
            {
                response.data[item.Key] = Convert.FromBase64String(item.Value);
            }
            return response;
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return true;
            //if (!validate) return true;
            //if (sslPolicyErrors == SslPolicyErrors.None)
            //{
            //    return true;
            //}
            //Utils.DoLog($"Certificate error: {sslPolicyErrors}");

            //// Do not allow this client to communicate with unauthenticated servers.
            //return false;
        }
    }
}
