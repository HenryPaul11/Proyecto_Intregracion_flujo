using System.Data;
using Microsoft.Data.SqlClient;
using ProyectoU1.Models;

namespace ProyectoU1.Data;

public sealed class PeliculaRepository(SqlConnectionFactory factory)
{
    private const string Columns = """
        f.film_id, f.title, f.description, f.release_year, f.language_id, f.rental_duration,
        f.rental_rate, f.length, f.replacement_cost, f.rating, f.activo,
        (SELECT COUNT(*) FROM dbo.inventory i WHERE i.film_id=f.film_id AND i.activo=1) AS Copias,
        (SELECT COUNT(*) FROM dbo.inventory i WHERE i.film_id=f.film_id AND i.activo=1
           AND NOT EXISTS (SELECT 1 FROM dbo.rental r WHERE r.inventory_id=i.inventory_id AND r.return_date IS NULL)) AS Disponibles
        """;

    private static Pelicula Read(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Titulo = r.GetString(1),
        Descripcion = r.IsDBNull(2) ? null : r.GetString(2),
        Anio = !r.IsDBNull(3) && int.TryParse(r.GetValue(3).ToString(), out var year) ? year : null,
        IdiomaId = r.GetByte(4),
        DuracionAlquiler = r.GetByte(5),
        Tarifa = r.GetDecimal(6),
        Duracion = r.IsDBNull(7) ? null : r.GetInt16(7),
        CostoReposicion = r.GetDecimal(8),
        Clasificacion = r.IsDBNull(9) ? "G" : r.GetString(9),
        Activo = r.GetBoolean(10),
        Copias = r.GetInt32(11),
        Disponibles = r.GetInt32(12)
    };

    public async Task<CatalogoModel> ListAsync(string? buscar, int pagina, bool admin)
    {
        pagina = Math.Max(1, pagina);
        await using var connection = await factory.OpenAsync();
        // Solo se compone SQL con constantes internas; los valores siempre se parametrizan.
        await using var command = new SqlCommand($"""
            SELECT COUNT(*) FROM dbo.film f WHERE (@admin=1 OR f.activo=1) AND f.title LIKE @q ESCAPE '\';
            SELECT {Columns} FROM dbo.film f WHERE (@admin=1 OR f.activo=1) AND f.title LIKE @q ESCAPE '\'
            ORDER BY f.title, f.film_id OFFSET @offset ROWS FETCH NEXT 24 ROWS ONLY;
            """, connection);
        command.Parameters.Add("@admin", SqlDbType.Bit).Value = admin;
        var search = (buscar ?? "").Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        command.Parameters.Add("@q", SqlDbType.NVarChar, 520).Value = $"%{search}%";
        command.Parameters.Add("@offset", SqlDbType.Int).Value = (pagina - 1) * 24;
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        var total = reader.GetInt32(0);
        await reader.NextResultAsync();
        var items = new List<Pelicula>();
        while (await reader.ReadAsync()) items.Add(Read(reader));
        return new CatalogoModel(items, buscar, pagina, total, admin);
    }

    public async Task<Pelicula?> GetAsync(int id)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand($"SELECT {Columns} FROM dbo.film f WHERE f.film_id=@id", connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<Opcion>> IdiomasAsync() => await OptionsAsync("SELECT language_id, RTRIM(name) FROM dbo.language ORDER BY name");
    public async Task<IReadOnlyList<Opcion>> TiendasAsync() => await OptionsAsync("SELECT store_id, CONCAT('Tienda ', store_id) FROM dbo.store ORDER BY store_id");

    private async Task<IReadOnlyList<Opcion>> OptionsAsync(string sql)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var options = new List<Opcion>();
        while (await reader.ReadAsync()) options.Add(new Opcion(Convert.ToInt32(reader.GetValue(0)), reader.GetString(1)));
        return options;
    }

    public async Task<int> SaveAsync(Pelicula film)
    {
        await using var connection = await factory.OpenAsync();
        var sql = film.Id == 0 ? """
            INSERT INTO dbo.film(title,description,release_year,language_id,rental_duration,rental_rate,length,replacement_cost,rating)
            OUTPUT INSERTED.film_id VALUES(@title,@description,@year,@language,@days,@rate,@length,@cost,@rating)
            """ : """
            UPDATE dbo.film SET title=@title, description=@description, release_year=@year, language_id=@language,
            rental_duration=@days, rental_rate=@rate, length=@length, replacement_cost=@cost, rating=@rating, last_update=GETDATE()
            OUTPUT INSERTED.film_id WHERE film_id=@id
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = film.Id;
        command.Parameters.Add("@title", SqlDbType.VarChar, 255).Value = film.Titulo.Trim();
        command.Parameters.Add("@description", SqlDbType.VarChar, 5000).Value = (object?)film.Descripcion ?? DBNull.Value;
        command.Parameters.Add("@year", SqlDbType.VarChar, 4).Value = (object?)film.Anio?.ToString() ?? DBNull.Value;
        command.Parameters.Add("@language", SqlDbType.TinyInt).Value = film.IdiomaId;
        command.Parameters.Add("@days", SqlDbType.TinyInt).Value = film.DuracionAlquiler;
        var rate = command.Parameters.Add("@rate", SqlDbType.Decimal); rate.Precision = 4; rate.Scale = 2; rate.Value = film.Tarifa;
        command.Parameters.Add("@length", SqlDbType.SmallInt).Value = (object?)film.Duracion ?? DBNull.Value;
        var cost = command.Parameters.Add("@cost", SqlDbType.Decimal); cost.Precision = 5; cost.Scale = 2; cost.Value = film.CostoReposicion;
        command.Parameters.Add("@rating", SqlDbType.VarChar, 10).Value = film.Clasificacion;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<bool> SetActiveAsync(int id, bool active)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("UPDATE dbo.film SET activo=@active, last_update=GETDATE() WHERE film_id=@id", connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@active", SqlDbType.Bit).Value = active;
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<IReadOnlyList<Copia>> CopiesAsync(int filmId)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT i.inventory_id, i.store_id, i.activo,
            CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.rental r WHERE r.inventory_id=i.inventory_id AND r.return_date IS NULL) THEN 1 ELSE 0 END AS bit)
            FROM dbo.inventory i WHERE i.film_id=@id ORDER BY i.inventory_id
            """, connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = filmId;
        await using var reader = await command.ExecuteReaderAsync();
        var copies = new List<Copia>();
        while (await reader.ReadAsync()) copies.Add(new Copia(reader.GetInt32(0), Convert.ToInt32(reader.GetValue(1)), reader.GetBoolean(2), reader.GetBoolean(3)));
        return copies;
    }

    public async Task<bool> AddCopyAsync(int filmId, int storeId)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            INSERT INTO dbo.inventory(film_id,store_id)
            SELECT @film,@store WHERE EXISTS(SELECT 1 FROM dbo.film WHERE film_id=@film AND activo=1)
                AND EXISTS(SELECT 1 FROM dbo.store WHERE store_id=@store)
            """, connection);
        command.Parameters.Add("@film", SqlDbType.Int).Value = filmId;
        command.Parameters.Add("@store", SqlDbType.Int).Value = storeId;
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeactivateCopyAsync(int filmId, int copyId)
    {
        await using var connection = await factory.OpenAsync();
        // El mismo bloqueo de la película se usa en alquileres, evitando retirar una copia mientras se asigna.
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var command = new SqlCommand("""
            SELECT film_id FROM dbo.film WITH(UPDLOCK,HOLDLOCK) WHERE film_id=@film;
            UPDATE dbo.inventory SET activo=0,last_update=GETDATE()
            WHERE inventory_id=@copy AND film_id=@film AND activo=1
              AND NOT EXISTS(SELECT 1 FROM dbo.rental WHERE inventory_id=@copy AND return_date IS NULL);
            SELECT @@ROWCOUNT;
            """, connection, transaction);
        command.Parameters.Add("@film", SqlDbType.Int).Value = filmId;
        command.Parameters.Add("@copy", SqlDbType.Int).Value = copyId;
        await using var reader = await command.ExecuteReaderAsync();
        await reader.NextResultAsync();
        await reader.ReadAsync();
        var updated = reader.GetInt32(0) > 0;
        await reader.DisposeAsync();
        await transaction.CommitAsync();
        return updated;
    }
}
