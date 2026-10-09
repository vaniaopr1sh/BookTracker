using BookTracker.Data;
using BookTracker.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Controllers.Api;

/// <summary>
/// JSON API книг. Видалення захищене JWT і доступне лише адміністратору.
/// </summary>
[ApiController]
[Route("api/books")]
public class BooksApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BooksApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/books/genres
    [HttpGet("genres")]
    public async Task<ActionResult<IEnumerable<string>>> Genres()
    {
        var genres = await _context.Books
            .Where(b => b.Genre != null && b.Genre != "")
            .Select(b => b.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
        return Ok(genres);
    }

    // DELETE: /api/books/5
    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound(new { message = "Книгу не знайдено або її вже видалено" });
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return Ok(new { id, message = $"Книгу «{book.Title}» видалено" });
    }
}
