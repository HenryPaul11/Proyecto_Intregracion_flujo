using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ProyectoU1.Data;
using ProyectoU1.Models;

// Prueba de integración: usa SQL Server real y deja sus registros con baja lógica.
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Sakila"] = "Server=localhost;Database=sakila;Integrated Security=True;TrustServerCertificate=True"
    })
    .AddEnvironmentVariables()
    .Build();
var factory = new SqlConnectionFactory(configuration);
var films = new PeliculaRepository(factory);
var rentals = new AlquilerRepository(factory);
await using var connection = await factory.OpenAsync();
await using var setup = new SqlCommand("""
    SELECT TOP (2) customer_id FROM dbo.customer WHERE active IN ('1','Y') ORDER BY customer_id;
    SELECT TOP (1) store_id FROM dbo.staff WHERE active=1 ORDER BY staff_id;
    SELECT TOP (1) language_id FROM dbo.language ORDER BY language_id;
    """, connection);
var customers = new List<int>();
int storeId;
byte languageId;
await using (var reader = await setup.ExecuteReaderAsync())
{
    while (await reader.ReadAsync()) customers.Add(reader.GetInt32(0));
    if (customers.Count != 2) throw new InvalidOperationException("Se necesitan dos clientes activos.");
    await reader.NextResultAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Se necesita un empleado activo.");
    storeId = Convert.ToInt32(reader.GetValue(0));
    await reader.NextResultAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Se necesita un idioma.");
    languageId = reader.GetByte(0);
}

var film = new Pelicula { Titulo = $"PRUEBA CONCURRENCIA {Guid.NewGuid():N}", IdiomaId = languageId };
var filmId = await films.SaveAsync(film);
try
{
    if (!await films.AddCopyAsync(filmId, storeId)) throw new InvalidOperationException("No se pudo crear la copia.");
    var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var requests = customers.Select(customer => Task.Run(async () =>
    {
        await start.Task;
        return await rentals.RentAsync(filmId, customer);
    })).ToArray();
    start.SetResult();
    var results = await Task.WhenAll(requests);
    if (results.Count(result => result.Exito) != 1)
        throw new InvalidOperationException("Debe aprobarse exactamente una de las dos solicitudes simultáneas.");

    await using var check = new SqlCommand("""
        SELECT COUNT(*) FROM dbo.rental r
        INNER JOIN dbo.inventory i ON i.inventory_id=r.inventory_id
        WHERE i.film_id=@film AND r.return_date IS NULL
        """, connection);
    check.Parameters.Add("@film", System.Data.SqlDbType.Int).Value = filmId;
    if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 1)
        throw new InvalidOperationException("La base de datos no contiene exactamente un alquiler pendiente.");
    Console.WriteLine("OK: dos solicitudes simultáneas, una sola copia asignada y un solo alquiler pendiente en SQL Server.");
}
finally
{
    // Devuelve solo los alquileres creados por esta prueba y conserva su historial.
    await using var query = new SqlCommand("""
        SELECT r.rental_id,r.customer_id FROM dbo.rental r
        INNER JOIN dbo.inventory i ON i.inventory_id=r.inventory_id
        WHERE i.film_id=@film AND r.return_date IS NULL
        """, connection);
    query.Parameters.Add("@film", System.Data.SqlDbType.Int).Value = filmId;
    var created = new List<(int Id, int Customer)>();
    await using (var reader = await query.ExecuteReaderAsync())
        while (await reader.ReadAsync()) created.Add((reader.GetInt32(0), reader.GetInt32(1)));
    foreach (var rental in created)
        await rentals.ReturnAsync(rental.Id, rental.Customer);
    foreach (var copy in await films.CopiesAsync(filmId))
        await films.DeactivateCopyAsync(filmId, copy.Id);
    await films.SetActiveAsync(filmId, false);
    Console.WriteLine($"Limpieza: película {filmId} y copias desactivadas; historial de prueba conservado.");
}
