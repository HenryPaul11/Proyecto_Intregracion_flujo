using System.Data;
using Microsoft.Data.SqlClient;
using ProyectoU1.Models;

namespace ProyectoU1.Data;

public sealed class AlquilerRepository(SqlConnectionFactory factory)
{
    public async Task<(bool Exito, string Mensaje)> RentAsync(int filmId, int customerId)
    {
        await using var connection = await factory.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var command = new SqlCommand("""
            -- Serializa las asignaciones de esta película hasta confirmar el alquiler.
            SELECT film_id FROM dbo.film WITH(UPDLOCK,HOLDLOCK) WHERE film_id=@film AND activo=1
            """, connection, transaction);
        command.Parameters.Add("@film", SqlDbType.Int).Value = filmId;
        if (await command.ExecuteScalarAsync() is null)
            return (false, "La película no existe o está inactiva.");

        command.CommandText = "SELECT customer_id FROM dbo.customer WHERE customer_id=@customer AND active IN ('1','Y')";
        command.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
        if (await command.ExecuteScalarAsync() is null)
            return (false, "La cuenta no está asociada a un cliente activo.");

        command.CommandText = """
            SELECT TOP (1) i.inventory_id FROM dbo.inventory i WITH(UPDLOCK,HOLDLOCK)
            WHERE i.film_id=@film AND i.activo=1
              AND EXISTS(SELECT 1 FROM dbo.staff s WHERE s.store_id=i.store_id AND s.active=1)
              AND NOT EXISTS(SELECT 1 FROM dbo.rental r WITH(UPDLOCK,HOLDLOCK)
                             WHERE r.inventory_id=i.inventory_id AND r.return_date IS NULL)
            ORDER BY i.inventory_id
            """;
        var copy = await command.ExecuteScalarAsync();
        if (copy is null)
            return (false, "No hay copias disponibles con un empleado activo para procesar el alquiler.");

        command.CommandText = """
            INSERT INTO dbo.rental(rental_date,inventory_id,customer_id,staff_id)
            SELECT GETDATE(),i.inventory_id,@customer,
                (SELECT TOP (1) s.staff_id FROM dbo.staff s WHERE s.store_id=i.store_id AND s.active=1 ORDER BY s.staff_id)
            FROM dbo.inventory i WHERE i.inventory_id=@copy
            """;
        command.Parameters.Add("@copy", SqlDbType.Int).Value = copy;
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return (true, "Alquiler registrado. La copia fue asignada a tu cuenta.");
    }

    public async Task<bool> ReturnAsync(int rentalId, int customerId)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            UPDATE dbo.rental SET return_date=GETDATE(),last_update=GETDATE()
            WHERE rental_id=@id AND customer_id=@customer AND return_date IS NULL
            """, connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = rentalId;
        command.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<AlquileresModel> ListAsync(int? customerId, int page)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT r.rental_id,i.inventory_id,f.title,CONCAT(c.first_name,' ',c.last_name),
                   r.rental_date,r.return_date,f.rental_duration
            FROM dbo.rental r
            INNER JOIN dbo.inventory i ON i.inventory_id=r.inventory_id
            INNER JOIN dbo.film f ON f.film_id=i.film_id
            INNER JOIN dbo.customer c ON c.customer_id=r.customer_id
            WHERE @customer IS NULL OR r.customer_id=@customer
            ORDER BY CASE WHEN r.return_date IS NULL THEN 0 ELSE 1 END, r.rental_date DESC, r.rental_id DESC
            OFFSET @offset ROWS FETCH NEXT 51 ROWS ONLY
            """, connection);
        command.Parameters.Add("@customer", SqlDbType.Int).Value = (object?)customerId ?? DBNull.Value;
        command.Parameters.Add("@offset", SqlDbType.Int).Value = (page - 1) * 50;
        await using var reader = await command.ExecuteReaderAsync();
        var rentals = new List<Alquiler>();
        while (await reader.ReadAsync()) rentals.Add(new Alquiler(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetString(3), reader.GetDateTime(4), reader.IsDBNull(5) ? null : reader.GetDateTime(5), reader.GetByte(6)));
        return new AlquileresModel(rentals.Take(50).ToList(), customerId is null, page, rentals.Count > 50);
    }
}
