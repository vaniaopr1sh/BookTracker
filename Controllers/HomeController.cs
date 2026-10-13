using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BookTracker.Models;

namespace BookTracker.Controllers;

public class HomeController : AppController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exception is not null)
        {
            _logger.LogError(exception.Error, "Необроблена помилка під час запиту {Path}", exception.Path);
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Сторінка для відповідей 400/403/404 тощо (див. UseStatusCodePagesWithReExecute у Program.cs).
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        var (title, message) = code switch
        {
            400 => ("Некоректний запит", "Запит містить некоректні дані."),
            403 => ("Доступ заборонено", "У вас немає прав для перегляду цієї сторінки."),
            404 => ("Сторінку не знайдено", "Такої сторінки або запису не існує — можливо, його було видалено."),
            _ => ("Помилка", "Під час обробки запиту сталася помилка.")
        };

        ViewData["Code"] = code;
        ViewData["Message"] = message;
        ViewData["Title"] = title;
        return View();
    }
}
