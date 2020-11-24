using System;
using JinrouClient.Domain;

namespace JinrouClient.Extensions
{
    public static class RoleExtensions
    {
        public static string ToName(this Role role)
        {
            return role switch
            {
                Role.Unkown => "",
                Role.Villager => "村人",
                Role.Werewolf => "人狼",
                _ => throw new ArgumentOutOfRangeException(nameof(role)),
            };
        }
    }
}
