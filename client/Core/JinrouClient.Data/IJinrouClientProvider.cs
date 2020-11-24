using System;
namespace JinrouClient.Data
{
    public interface IJinrouClientProvider
    {
        Jinrou.Jinrou.JinrouClient Client { get; }
    }
}
