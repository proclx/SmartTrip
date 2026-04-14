using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTrip.Models;

namespace SmartTrip.Application.Interfaces
{
    public interface IEventDiscoveryService
    {
        Task<IEnumerable<LocalEvent>> GetEventsAsync(int cityId, string cityName, DateTime startDate, DateTime endDate);
    }
}