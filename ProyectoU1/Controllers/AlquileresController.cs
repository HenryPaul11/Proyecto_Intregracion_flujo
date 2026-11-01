using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoU1.Data;
using ProyectoU1.Filters;

namespace ProyectoU1.Controllers;

[SessionAuthorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AlquileresController(AlquilerRepository repository) : Controller
{
    [SessionAuthorize("Cliente")]
    public async Task<IActionResult> Index(int pagina = 1)
    {
        var customerId = HttpContext.Session.GetInt32("CustomerId");
        if (!customerId.HasValue) return StatusCode(403);
        return View(await repository.ListAsync(customerId, Math.Clamp(pagina, 1, 100000)));
    }

    [SessionAuthorize("Administrador")]
    public async Task<IActionResult> Todos(int pagina = 1)
        => View("Index", await repository.ListAsync(null, Math.Clamp(pagina, 1, 100000)));

    [SessionAuthorize("Cliente"), HttpPost]
    public async Task<IActionResult> Alquilar(int peliculaId)
    {
        var customerId = HttpContext.Session.GetInt32("CustomerId");
        if (!customerId.HasValue) return StatusCode(403);
        if (!ModelState.IsValid || peliculaId < 1) return BadRequest();
        try
        {
            var result = await repository.RentAsync(peliculaId, customerId.Value);
            TempData[result.Exito ? "Exito" : "Error"] = result.Mensaje;
            return result.Exito ? RedirectToAction(nameof(Index)) : RedirectToAction("Detalle", "Peliculas", new { id = peliculaId });
        }
        catch (SqlException ex) when (ex.Number is 1205 or 2601 or 2627)
        {
            TempData["Error"] = "La disponibilidad cambió mientras se procesaba el alquiler. Inténtalo nuevamente.";
            return RedirectToAction("Detalle", "Peliculas", new { id = peliculaId });
        }
    }

    [SessionAuthorize("Cliente"), HttpPost]
    public async Task<IActionResult> Devolver(int id)
    {
        var customerId = HttpContext.Session.GetInt32("CustomerId");
        if (!customerId.HasValue) return StatusCode(403);
        if (!ModelState.IsValid || id < 1) return BadRequest();
        var returned = await repository.ReturnAsync(id, customerId.Value);
        TempData[returned ? "Exito" : "Error"] = returned ? "Devolución registrada. Se conserva el historial." : "El alquiler no te pertenece, no existe o ya fue devuelto.";
        return RedirectToAction(nameof(Index));
    }
}
