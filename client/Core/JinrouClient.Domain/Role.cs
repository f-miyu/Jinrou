using System;
namespace JinrouClient.Domain
{
    public enum Role
    {
        Unkown,
        Villager,
        Werewolf,
    }

    public static class RoleExtensions
    {
        public static Side ToSide(this Role role) => role switch
        {
            Role.Unkown => Side.Neutral,
            Role.Villager => Side.Villagers,
            Role.Werewolf => Side.Werewolves,
            _ => Side.Neutral
        };
    }
}
