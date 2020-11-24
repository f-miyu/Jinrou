using System;
using JinrouClient.Domain;

namespace JinrouClient.Models
{
    public record PlayerData : Player
    {
        public bool IsMe { get; init; }
        public bool CanSelect { get; init; }
        public ActionType ActionType { get; init; }
        public string? Status { get; init; }
    }
}
