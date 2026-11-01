using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProyectoU1.Data;
using ProyectoU1.Models;

namespace ProyectoU1.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CuentaController(UsuarioRepository repository, IPasswordHasher<Usuario> hasher) : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginModel());

    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await repository.FindAsync(model.Login.Trim());
        // También se calcula un hash para usuarios inexistentes, reduciendo diferencias de tiempo.
        var result = user is null
            ? hasher.VerifyHashedPassword(new Usuario(), hasher.HashPassword(new Usuario(), "inexistente"), model.Password)
            : hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(model);
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            await repository.UpdateHashAsync(user.Id, hasher.HashPassword(user, model.Password));
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UsuarioId", user.Id);
        HttpContext.Session.SetString("Nombre", user.Nombre);
        HttpContext.Session.SetString("Rol", user.Rol);
        if (user.CustomerId.HasValue) HttpContext.Session.SetInt32("CustomerId", user.CustomerId.Value);
        if (user.StaffId.HasValue) HttpContext.Session.SetInt32("StaffId", user.StaffId.Value);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Response.Cookies.Delete(".Sakila.Session");
        return RedirectToAction(nameof(Login));
    }
}
