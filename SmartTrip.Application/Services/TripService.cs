using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartTrip.Data;
using SmartTrip.Models;
using SmartTrip.Application.Interfaces;

namespace SmartTrip.Application.Services
{
    public class TripService : ITripService
    {
        private readonly SmartTripDbContext _context;

        public TripService(SmartTripDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateTripAsync(string userId, string destinationName, DateTime startDate, DateTime endDate)
        {
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.Name.ToLower() == destinationName.ToLower());

            if (city == null)
            {
                city = new City
                {
                    Name = destinationName,
                    Country = "Не вказано" 
                };

                _context.Cities.Add(city);
                await _context.SaveChangesAsync();
            }

            var trip = new Trip
            {
                UserId = userId,
                CityId = city.Id,
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return trip.Id;
        }
    }
}
