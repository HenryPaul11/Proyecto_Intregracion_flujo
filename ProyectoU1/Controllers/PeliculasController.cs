using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoU1.Data;
using ProyectoU1.Filters;
using ProyectoU1.Models;

namespace ProyectoU1.Controllers;

[SessionAuthorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PeliculasController(PeliculaRepository repository) : Controller
{
    public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        => View(await repository.ListAsync(buscar?[..Math.Min(buscar.Length, 255)], Math.Clamp(pagina, 1, 100000), false));

    [SessionAuthorize("Administrador")]
    public async Task<IActionResult> Administrar(string? buscar, int pagina = 1)
        => View("Index", await repository.ListAsync(buscar?[..Math.Min(buscar.Length, 255)], Math.Clamp(pagina, 1, 100000), true));

    public async Task<IActionResult> Detalle(int id)
    {
        var film = await repository.GetAsync(id);
        if (film is null || (!film.Activo && HttpContext.Session.GetString("Rol") != "Administrador")) return NotFound();
        return View(film);
    }

    [SessionAuthorize("Administrador"), HttpGet]
    public async Task<IActionResult> Crear()
    {
        ViewBag.Idiomas = await repository.IdiomasAsync();
        return View("Editar", new Pelicula());
    }

    [SessionAuthorize("Administrador"), HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var film = await repository.GetAsync(id);
        if (film is null) return NotFound();
        ViewBag.Idiomas = await repository.IdiomasAsync();
        return View(film);
    }

    [SessionAuthorize("Administrador"), HttpPost]
    public async Task<IActionResult> Guardar(Pelicula model)
    {
        var languages = await repository.IdiomasAsync();
        if (!languages.Any(x => x.Id == model.IdiomaId)) ModelState.AddModelError(nameof(model.IdiomaId), "Seleccione un idioma válido.");
        if (model.Id != 0 && await repository.GetAsync(model.Id) is null) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                var id = await repository.SaveAsync(model);
                TempData["Exito"] = "Película guardada correctamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            catch (SqlException ex) when (ex.Number is 547 or 2628 or 8152)
            { ModelState.AddModelError("", "Revise los valores y las relaciones de la película."); }
        }
        ViewBag.Idiomas = languages;
        return View("Editar", model);
    }

    [SessionAuthorize("Administrador"), HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        if (!ModelState.IsValid || id < 1) return BadRequest();
        if (!await repository.SetActiveAsync(id, activo)) return NotFound();
        TempData["Exito"] = activo ? "Película reactivada." : "Película desactivada. Se conserva su historial.";
        return RedirectToAction(nameof(Administrar));
    }

    [SessionAuthorize("Administrador")]
    public async Task<IActionResult> Inventario(int id)
    {
        var film = await repository.GetAsync(id);
        if (film is null) return NotFound();
        return View(new InventarioModel(film, await repository.CopiesAsync(id), await repository.TiendasAsync()));
    }

    [SessionAuthorize("Administrador"), HttpPost]
    public async Task<IActionResult> AgregarCopia(int id, int tiendaId)
    {
        if (!ModelState.IsValid || !await repository.AddCopyAsync(id, tiendaId)) TempData["Error"] = "Seleccione una tienda válida y una película activa.";
        else TempData["Exito"] = "Copia registrada.";
        return RedirectToAction(nameof(Inventario), new { id });
    }

    [SessionAuthorize("Administrador"), HttpPost]
    public async Task<IActionResult> RetirarCopia(int id, int copiaId)
    {
        if (!ModelState.IsValid || !await repository.DeactivateCopyAsync(id, copiaId)) TempData["Error"] = "No se puede retirar una copia alquilada, inactiva o inexistente.";
        else TempData["Exito"] = "Copia retirada mediante eliminación lógica.";
        return RedirectToAction(nameof(Inventario), new { id });
    }
}
