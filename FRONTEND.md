# BookWings — Frontend (завершено)

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

7. **Валідація та обробка помилок** — єдині українські повідомлення в браузері (`validation-uk.js`) і на сервері
   (повідомлення прив'язки даних у `Program.cs`); фокус на першому полі з помилкою, захист від повторного надсилання;
   сторінки 404 / 403 / 500 у фірмовому стилі; для `/api/*` помилки повертаються у JSON;
   повідомлення про успіх і помилки для дій з полицею.
8. **Особистий кабінет** (`/Profile`) — дані користувача, ролі, зміна імені з валідацією;
   статистика читача з захищеного JWT endpoint `GET /api/profile/stats` (кількість книг, прогрес, середня оцінка,
   улюблений жанр, останні книги); для адміністратора окрема панель зі статистикою порталу;
   інформація про сесію та вихід.

9. **Розмежування ролей та адміністративна зона** — окрема область `Areas/Admin` (`/Admin`), усі сторінки якої
   закриті роллю «Адміністратор» (`AdminControllerBase`); гість бачить popup входу, звичайний користувач — сторінку 403;
   адміністративні API (`/api/admin/*`, `DELETE /api/books`) перевіряють роль у JWT; посилання на адмінку в меню бачить лише адміністратор.
10. **Адміністративна панель** — дашборд (книги, користувачі, оцінки, полиці; розподіл за жанрами і статусами;
    топ за рейтингом; останні додані; книги без опису чи обкладинки); таблиця книг з пошуком, фільтром за жанром,
    сторінками, редагуванням і видаленням; керування користувачами (пошук, надання/зняття прав адміністратора через
    `POST /api/admin/users/{id}/role`, свою роль змінити не можна).

11. **Інтеграція та стабілізація** — повідомлення ASP.NET Identity українською (`UkrainianIdentityErrorDescriber`),
    перевірка надійності пароля в браузері за тими самими правилами, що й на сервері; сторінки налаштувань
    облікового запису (контактні дані, зміна пароля) перекладені й оформлені у фірмовому стилі;
    виправлено повернення на полицю після зміни статусу; назву ролі в JS більше не продубльовано;
    пагінація каталогу («Показати ще») для великої кількості книг.
12. **Фінальне доопрацювання** — нова сторінка «Моя полиця» (картки, вкладки-фільтри за статусом з лічильниками),
    блок «Вам також може сподобатися» на сторінці книги, кнопка «Вгору», швидкий вхід / кабінет на телефоні
    без відкриття меню, підвищено контраст дрібного тексту до рівня WCAG AA, мета-теги для соцмереж,
    версія сторінок для друку, файл рішення `BookTracker.sln` для Visual Studio.

Додатково: клієнтська валідація, сторінки 403 / помилки, адаптивність (мобільні), доступність (aria, фокус).

## Нові / змінені файли
```
Controllers/Api/AuthApiController.cs   — /api/auth/login|register|logout|token|me
Controllers/Api/BooksApiController.cs  — /api/books/genres, DELETE /api/books/{id}
Controllers/Api/ProfileApiController.cs — /api/profile/stats (JWT)
Controllers/ProfileController.cs, Services/ProfileStatsService.cs, ViewModels/ProfileViewModel.cs
Views/Profile/Index.cshtml, Views/Home/StatusCode.cshtml, ViewModels/StatusCodeViewModel.cs
wwwroot/js/profile.js, wwwroot/js/validation-uk.js
Areas/Admin/Controllers/AdminControllerBase.cs, DashboardController.cs, CatalogController.cs, UsersController.cs
Areas/Admin/Views/Shared/_AdminLayout.cshtml, Dashboard/Index, Catalog/Index, Users/Index
Controllers/Api/AdminApiController.cs, ViewModels/AdminViewModels.cs, wwwroot/js/admin.js
Services/UkrainianIdentityErrorDescriber.cs
Areas/Identity/Pages/Account/Manage/_Layout, _ManageNav, Index, ChangePassword (+ .cs)
BookTracker.sln
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

## Перевірка
Проєкт зібрано (`dotnet build`, без помилок) і запущено. Автоматичний браузерний сценарій (50 перевірок) пройдено повністю:
вхід / реєстрація у popup, JWT, перевірка пароля, живий пошук і «Показати ще», створення / редагування / видалення книги,
валідація форм, полиця (додавання, статус, оцінка, фільтр, видалення з підтвердженням), схожі книги, особистий кабінет,
зміна пароля, помилки Identity українською, захист /Admin для гостя і користувача, заборона адмін-API для звичайного
користувача, зміна ролі, вихід, сторінка 404, мобільна верстка (без горизонтальної прокрутки, вхід без меню),
відсутність помилок JavaScript.
