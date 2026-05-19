using System;
using System.Collections.Generic;

namespace SmartTrip.Models
{
    public class VotingSession
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public Trip Trip { get; set; }

        public Guid ShareToken { get; set; } = Guid.NewGuid();
        public int PeopleCount { get; set; }
        public bool IsCompleted { get; set; }

        public ICollection<VotingItem> VotingItems { get; set; } = new List<VotingItem>();
    }
}