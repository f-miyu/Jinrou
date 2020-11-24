using System;
using Grpc.Net.Client;

namespace JinrouClient.Data
{
    public class DotNetJinrouClientProvider : IJinrouClientProvider
    {
        public DotNetJinrouClientProvider(string address)
        {
            var channel = GrpcChannel.ForAddress(address);
            Client = new Jinrou.Jinrou.JinrouClient(channel);
        }

        public Jinrou.Jinrou.JinrouClient Client { get; }
    }
}
