using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProyectoU1.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute(string? rol = null) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.GetInt32("UsuarioId").HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Cuenta", null);
            return;
        }
        if (rol is not null && session.GetString("Rol") != rol)
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
