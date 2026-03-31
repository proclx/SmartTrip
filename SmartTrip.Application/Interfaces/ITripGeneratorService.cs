using System.Threading.Tasks;

namespace SmartTrip.Application.Interfaces
{
    public interface ITripGeneratorService
    {
        Task GenerateItineraryAsync(int tripId);
    }
}
