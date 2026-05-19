using System.Collections.Generic;

namespace SmartTrip.Models
{
    public class VotingItem
    {
        public int Id { get; set; }
        public int VotingSessionId { get; set; }
        public VotingSession VotingSession { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; } // Наприклад: "Музей", "Бар", "Екстрим"

        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}