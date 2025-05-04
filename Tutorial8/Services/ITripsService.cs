using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<ClientTripDTO>> GetTripsForClient(int clientId);
    
    Task<int> CreateClient(ClientCreateDTO clientDto);
    
    Task<bool> AssignClientToTrip(ClientTripAssignDTO assignDto);
    
    Task<bool> RemoveClientFromTrip(int clientId, int tripId);
    
}