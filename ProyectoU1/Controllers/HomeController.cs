using Microsoft.AspNetCore.Mvc;
using ProyectoU1.Models;
using System.Diagnostics;

namespace ProyectoU1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [IgnoreAntiforgeryToken]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Estado(int id)
        {
            if (id < 400 || id > 599) id = StatusCodes.Status404NotFound;
            Response.StatusCode = id;
            return View(id);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
