namespace Tutorial8.Models.DTOs;

public class ClientTripAssignDTO
{
    public int IdClient { get; set; }
    
    public int IdTrip { get; set; }
    
    public string? PaymentDate { get; set; }
}