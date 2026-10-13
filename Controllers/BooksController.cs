using BookTracker.Models;
using BookTracker.Services;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers;

public class BooksController : AppController
{
    private readonly IBookService _bookService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BooksController(IBookService bookService, UserManager<ApplicationUser> userManager)
    {
        _bookService = bookService;
        _userManager = userManager;
    }

    // GET: /Books?search=...&genre=...&yearFrom=...&yearTo=...&sort=...
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] BookQuery query)
    {
        // Некоректні параметри не ламають сторінку: сервіс їх нормалізує, а помилки показуються над формою.
        var model = await _bookService.GetCatalogAsync(query, _userManager.GetUserId(User));
        return View(model);
    }

    // GET: /Books/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _bookService.GetDetailsAsync(id, _userManager.GetUserId(User));
        if (model is null) return NotFound();
        return View(model);
    }

    // GET: /Books/Create
    [Authorize(Roles = Roles.Administrator)]
    public IActionResult Create() => View();

    // POST: /Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Create([Bind("Title,Author,Genre,PublicationYear,Description,CoverImageUrl")] Book book)
    {
        if (!ModelState.IsValid) return View(book);

        var result = await _bookService.CreateAsync(book);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(book);
        }

        ShowSuccess($"Книгу «{book.Title}» додано до каталогу.");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Books/Edit/5
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _bookService.FindAsync(id);
        if (book is null) return NotFound();
        return View(book);
    }

    // POST: /Books/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Genre,PublicationYear,Description,CoverImageUrl")] Book book)
    {
        if (id != book.Id) return BadRequest();
        if (!ModelState.IsValid) return View(book);

        var result = await _bookService.UpdateAsync(book);
        if (result.ErrorType == ServiceErrorType.NotFound)
        {
            ShowError(result.Error!);
            return RedirectToAction(nameof(Index));
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(book);
        }

        ShowSuccess($"Зміни у книзі «{book.Title}» збережено.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Books/Delete/5
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _bookService.FindAsync(id);
        if (book is null) return NotFound();
        return View(book);
    }

    // POST: /Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _bookService.DeleteAsync(id);
        ShowResult(result, "Книгу видалено з каталогу.");
        return RedirectToAction(nameof(Index));
    }
}
