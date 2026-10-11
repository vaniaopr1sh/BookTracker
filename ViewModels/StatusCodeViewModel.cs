namespace BookTracker.ViewModels;

/// <summary>Дані для сторінки помилки з HTTP-кодом (404, 403, 500 тощо).</summary>
public class StatusCodeViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static StatusCodeViewModel For(int code) => code switch
    {
        400 => new() { Code = code, Title = "Некоректний запит", Message = "Сервер не зміг обробити запит. Перевірте введені дані." },
        401 => new() { Code = code, Title = "Потрібна авторизація", Message = "Увійдіть до свого облікового запису, щоб продовжити." },
        403 => new() { Code = code, Title = "Доступ обмежено", Message = "Недостатньо прав для перегляду цієї сторінки." },
        404 => new() { Code = code, Title = "Сторінку не знайдено", Message = "Схоже, ця сторінка загубилася між книжковими полицями. Можливо, її видалили або посилання неправильне." },
        _ => new() { Code = code, Title = "Щось пішло не так", Message = "Сталася помилка сервера. Спробуйте пізніше." }
    };
}
