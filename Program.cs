using System.Text;
using BookTracker.Data;
using BookTracker.Models;
using BookTracker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<UkrainianIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Cookie лишається основною схемою для MVC-сторінок; неавторизованого
// користувача повертаємо на головну, де автоматично відкривається popup входу.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// JWT — для клієнтської частини (fetch-запити до /api/*).
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Section 'Jwt' not found.");

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IProfileStatsService, ProfileStatsService>();
builder.Services.AddControllersWithViews(options =>
{
    // Українські повідомлення про помилки прив'язки даних (однакові для всіх форм)
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustBeANumberAccessor(field => $"Поле «{field}» має бути числом");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значення «{value}» некоректне для поля «{field}»");
    messages.SetValueIsInvalidAccessor(value => $"Значення «{value}» некоректне");
    messages.SetMissingBindRequiredValueAccessor(field => $"Не передано значення для поля «{field}»");
    messages.SetValueMustNotBeNullAccessor(field => $"Поле «{field}» не може бути порожнім");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Некоректне значення поля «{field}»");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "Значення має бути числом");
    messages.SetMissingRequestBodyRequiredValueAccessor(() => "Тіло запиту порожнє");
});

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

// Сторінки помилок 404 / 403 / 500 у фірмовому стилі.
// Для запитів до /api відповідь повертається у форматі JSON (див. HomeController.StatusCodePage).
app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Адміністративна зона: /Admin, /Admin/Catalog, /Admin/Users
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

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
