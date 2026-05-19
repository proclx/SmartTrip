using System;

namespace SmartTrip.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public int VotingItemId { get; set; }
        public VotingItem VotingItem { get; set; }

        // Ідентифікатор виборця (оскільки друзі без акаунтів, можна генерувати тимчасовий guid на їхньому пристрої (у кукі/localstorage))
        public string VoterId { get; set; } 
        public bool IsLiked { get; set; } // true = 👍 (Хочу туди), false = 👎 (Нудно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}