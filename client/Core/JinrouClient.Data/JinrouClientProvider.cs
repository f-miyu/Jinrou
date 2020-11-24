using System;
using Grpc.Core;
using Jinrou;

namespace JinrouClient.Data
{
    public class JinrouClientProvider : IJinrouClientProvider
    {
        public JinrouClientProvider(string address)
        {
            var channel = new Channel(address, ChannelCredentials.Insecure);
            Client = new Jinrou.Jinrou.JinrouClient(channel);
        }

        public Jinrou.Jinrou.JinrouClient Client { get; }
    }
}
