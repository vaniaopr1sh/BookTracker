using BookTracker.Data;
using BookTracker.Models;
using BookTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Зміна ролі або блокування користувача застосовується до відкритих сесій не пізніше ніж за хвилину.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IUserBookService, UserBookService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();

builder.Services.AddControllersWithViews(options =>
{
    // Українські повідомлення для помилок перетворення значень (наприклад, «abc» у полі року).
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustBeANumberAccessor(field => $"Поле «{field}» має бути числом.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значення «{value}» некоректне для поля «{field}».");
    messages.SetValueIsInvalidAccessor(value => $"Значення «{value}» некоректне.");
    messages.SetValueMustNotBeNullAccessor(field => $"Поле «{field}» обов'язкове.");
    messages.SetMissingBindRequiredValueAccessor(field => $"Не передано значення для «{field}».");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Значення «{value}» некоректне.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Указано некоректне значення для «{field}».");
});

// Не перетворювати кирилицю на HTML-сутності (&#x...;) у відрендерених сторінках.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Відповіді 400/403/404 без тіла показуються зрозумілою сторінкою замість порожньої.
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
