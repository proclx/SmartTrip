using System;
using System.Threading.Tasks;

namespace SmartTrip.Application.Interfaces
{
    public interface ITripService
    {
        Task<int> CreateTripAsync(string userId, string destinationName, DateTime startDate, DateTime endDate);
    }
}
