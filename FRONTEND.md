# BookWings — Frontend (етап ~50%)

Фронтенд книжкового порталу **BookWings** на ASP.NET Core MVC (Razor + Bootstrap 5 + vanilla JS),
оформлений за брендбуком BookWings 2026.

## Фірмовий стиль (з брендбуку)
| Колір | HEX | Де використано |
|---|---|---|
| Крем | `#ECE9E2` | фон сайту |
| Пісок | `#D1D0BD` | рамки, скелетони |
| Оливка | `#ACAC85` | хвилі, чіпи, акценти |
| Кора | `#58432E` | кнопки, заголовки |
| Темний ліс | `#33271B` | футер, заглушки обкладинок, popup |

* Шрифти: **Alata** — назва BookWings, **Montserrat** — увесь інший текст (Google Fonts).
* Логотип, виворітка логотипу, знак «крила W», фірмовий патерн з розгорнутих книжок, хвилі, персонаж **Alata**.
* Слоган: «Книги, які дарують крила мрій».

## Що реалізовано (консультації 1–6 з плану)
1. **Базова структура та авторизація** — нова головна сторінка, Login / Registration у **popup**, JWT, обмін даними з Backend через `fetch`.
2. **Автентифікація** — повний цикл Login / Registration / Logout; збереження й відновлення сесії;
   централізований `wwwroot/js/auth.js` (токен автоматично додається в `Authorization: Bearer`);
   захищені маршрути (неавторизованого користувача повертає на головну з відкритим popup);
   реакція на завершення/втрату сесії, синхронізація між вкладками.
3. **Каталог** — картки книг (`_BookCard`), навігація, перехід до книги, стани завантаження (скелетон) та «нічого не знайдено».
4. **Детальна сторінка книги, пошук** — повна інформація, рейтинг зірками, полиця читача;
   живий пошук, фільтр за жанром, сортування (без перезавантаження сторінки).
5. **Створення та редагування** — спільна форма `_BookForm` з попереднім переглядом обкладинки, лічильниками символів, підказками жанрів з API.
6. **Видалення** — popup-підтвердження, видалення через захищений JWT endpoint `DELETE /api/books/{id}`, оновлення каталогу без перезавантаження, toast-повідомлення про успіх/помилку.

Додатково: клієнтська валідація, сторінки 403 / помилки, адаптивність (мобільні), доступність (aria, фокус).

## Нові / змінені файли
```
Controllers/Api/AuthApiController.cs   — /api/auth/login|register|logout|token|me
Controllers/Api/BooksApiController.cs  — /api/books/genres, DELETE /api/books/{id}
Services/JwtOptions.cs, Services/JwtTokenService.cs
ViewModels/AuthDtos.cs, CatalogViewModel.cs, HomeViewModel.cs
Views/Shared/_Layout, _LoginPartial, _AuthModal, _ConfirmModal, _BookCard, _BookCover, _Stars, Error
Views/Home/Index, Privacy, AccessDenied
Views/Books/Index, _BookGrid, Details, _BookForm, Create, Edit, Delete
Views/MyBooks/Index, Areas/Identity/.../Login, Register (резервні сторінки)
wwwroot/css/site.css, wwwroot/js/auth.js, site.js, catalog.js, book-form.js, wwwroot/img/*
Program.cs, appsettings.json (секція Jwt), BookTracker.csproj (пакет JwtBearer), Models/Book.cs (українські підписи/валідація)
```

## Запуск
```
dotnet restore
dotnet run
```
Адміністратор (створюється автоматично): `admin@booktracker.local` / `Admin123!`

> Ключ `Jwt:Key` в `appsettings.json` — лише для розробки, для продакшну замініть його.
