using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace SmartTrip.Models
{
    public class User : IdentityUser
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? ProfileImageUrl { get; set; }

        public List<Photo> Photos { get; set; } = new();
    }
}
