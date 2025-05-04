using System.Globalization;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = @"
        SELECT t.IdTrip, t.Name, c.IdCountry, c.Name as CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.Name";
    
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var currentTripId = -1;
                TripDTO currentTrip = null;
            
                while (await reader.ReadAsync())
                {
                    var tripId = reader.GetInt32(0);
                
                    if (tripId != currentTripId)
                    {
                        currentTrip = new TripDTO()
                        {
                            Id = tripId,
                            Name = reader.GetString(1),
                            Countries = new List<CountryDTO>()
                        };
                        trips.Add(currentTrip);
                        currentTripId = tripId;
                    }
                
                    if (!reader.IsDBNull(2))
                    {
                        currentTrip.Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(3)
                        });
                    }
                }
            }
        }

        return trips;
    }
       public async Task<List<ClientTripDTO>> GetTripsForClient(int clientId)
    {
        var result = new List<ClientTripDTO>();

        string query = @"
        SELECT t.Name, t.Description, t.DateFrom, t.DateTo,
               ct.RegisteredAt, ct.PaymentDate
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        WHERE ct.IdClient = @IdClient";

        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", clientId);

            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var trip = new ClientTripDTO
                    {
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        RegisteredAt = DateTime.ParseExact(
                            reader.GetInt32(reader.GetOrdinal("RegisteredAt")).ToString(),
                            "yyyyMMdd",
                            CultureInfo.InvariantCulture
                        ),
                        PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                            ? (DateTime?)null
                            : DateTime.ParseExact(
                                reader.GetInt32(reader.GetOrdinal("PaymentDate")).ToString(),
                                "yyyyMMdd",
                                CultureInfo.InvariantCulture
                            )
                    };

                    result.Add(trip);
                }
            }
        }

        return result;
    }
    public async Task<int> CreateClient(ClientCreateDTO clientDto)
    {
        string query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
    
        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", clientDto.LastName);
            cmd.Parameters.AddWithValue("@Email", clientDto.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Telephone", clientDto.Telephone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Pesel", clientDto.Pesel ?? (object)DBNull.Value);
        
            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync();
        }
    }
    
    public async Task<bool> AssignClientToTrip(ClientTripAssignDTO assignDto)
{
    string clientCheck = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
    string tripCheck = "SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
    string existingCheck = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
    
    using (var conn = new SqlConnection(_connectionString))
    {
        await conn.OpenAsync();
        
        using (var cmd = new SqlCommand(clientCheck, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", assignDto.IdClient);
            if (await cmd.ExecuteScalarAsync() == null)
                return false;
        }
        
        using (var cmd = new SqlCommand(tripCheck, conn))
        {
            cmd.Parameters.AddWithValue("@IdTrip", assignDto.IdTrip);
            if (await cmd.ExecuteScalarAsync() == null)
                return false;
        }
        

        using (var cmd = new SqlCommand(existingCheck, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", assignDto.IdClient);
            cmd.Parameters.AddWithValue("@IdTrip", assignDto.IdTrip);
            if (await cmd.ExecuteScalarAsync() != null)
                return false;
        }
        
        string insertQuery = @"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
            VALUES (@IdClient, @IdTrip, @RegisteredAt, @PaymentDate)";
            
        using (var cmd = new SqlCommand(insertQuery, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", assignDto.IdClient);
            cmd.Parameters.AddWithValue("@IdTrip", assignDto.IdTrip);
            cmd.Parameters.AddWithValue("@RegisteredAt", DateTime.Now.ToString("yyyyMMdd"));
            
            if (string.IsNullOrEmpty(assignDto.PaymentDate))
                cmd.Parameters.AddWithValue("@PaymentDate", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@PaymentDate", assignDto.PaymentDate);
                
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
    
    public async Task<bool> RemoveClientFromTrip(int clientId, int tripId)
    {
        string query = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
    
        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", clientId);
            cmd.Parameters.AddWithValue("@IdTrip", tripId);
        
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}