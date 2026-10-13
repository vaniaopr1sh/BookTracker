using BookTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Areas.Admin.Controllers;

/// <summary>
/// Базовий контролер адміністративної зони.
/// Усі сторінки /Admin/* доступні лише користувачам з роллю «Адміністратор»;
/// звичайний користувач отримує сторінку «Доступ обмежено», гість — popup входу.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
public abstract class AdminControllerBase : Controller
{
}
