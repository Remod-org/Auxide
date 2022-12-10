using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Auxide
{
    public class SubscriptionClient: WebClient
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
            _httpClient = new HttpClient() { BaseAddress = new Uri("https://code.remod.org/")};
            _username = Auxide.config.Options.subscription.username;
            _password = Auxide.config.Options.subscription.password;

            if (Auxide.config.Options.subscription.enabled) AuxideSub();
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
    }
}
