using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoU1.Data;
using ProyectoU1.Filters;
using ProyectoU1.Models;

namespace ProyectoU1.Controllers;

[SessionAuthorize("Administrador"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReportesController(SqlConnectionFactory factory) : Controller
{
    public async Task<IActionResult> PeliculasMasAlquiladas()
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT TOP (10) f.title, COUNT(*) AS Cantidad
            FROM dbo.film f
            INNER JOIN dbo.inventory i ON i.film_id=f.film_id
            INNER JOIN dbo.rental r ON r.inventory_id=i.inventory_id
            GROUP BY f.film_id,f.title
            ORDER BY Cantidad DESC,f.title,f.film_id
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<PeliculaReporte>();
        while (await reader.ReadAsync()) results.Add(new PeliculaReporte(reader.GetString(0), reader.GetInt32(1)));
        return View(results);
    }
}
