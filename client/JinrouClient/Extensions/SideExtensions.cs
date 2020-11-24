using System;
using JinrouClient.Domain;

namespace JinrouClient.Extensions
{
    public static class SideExtensions
    {
        public static string ToName(this Side side)
        {
            return side switch
            {
                Side.Neutral => "",
                Side.Villagers => "村人陣営",
                Side.Werewolves => "人狼陣営",
                _ => throw new ArgumentOutOfRangeException(nameof(side)),
            };
        }
    }
}
