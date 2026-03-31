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

            int totalDays = (endDate.Date - startDate.Date).Days + 1;

            for (int i = 0; i < totalDays; i++)
            {
                var tripDay = new TripDay 
                {
                    TripId = trip.Id,
                    Date = startDate.AddDays(i).ToUniversalTime(),
                    DayNumber = i + 1
                };

                _context.TripDays.Add(tripDay); 
            }

            await _context.SaveChangesAsync();

            return trip.Id;
        }

        public async Task<IEnumerable<Trip>> GetUserTripsAsync(string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Trip>> GetFavoriteTripsAsync(string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .Where(t => t.UserId == userId && t.IsFavorite)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<Trip?> GetTripByIdAsync(int tripId, string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);
        }

        public async Task<bool> UpdateTripAsync(int tripId, string userId, int peopleCount, int? rating)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);
            if (trip == null)
                return false;

            trip.PeopleCount = peopleCount;
            trip.Rating = rating;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFavoriteAsync(int tripId, string userId)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);
            if (trip == null)
                return false;

            trip.IsFavorite = !trip.IsFavorite;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
