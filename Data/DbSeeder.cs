using BookTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Data;

public static class DbSeeder
{
    private const string AdminEmail = "admin@booktracker.local";
    private const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.User, Roles.Administrator })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                DisplayName = "Адміністратор",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Administrator);
            }
        }
        else if (!await userManager.IsInRoleAsync(admin, Roles.Administrator))
        {
            await userManager.AddToRoleAsync(admin, Roles.Administrator);
        }

        // Кожен обліковий запис має мати роль: користувачам без ролі призначаємо «Користувач».
        var usersWithoutRole = await db.Users
            .Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id))
            .ToListAsync();
        foreach (var user in usersWithoutRole)
        {
            await userManager.AddToRoleAsync(user, Roles.User);
        }

        if (!await db.Books.AnyAsync())
        {
            db.Books.AddRange(
                new Book { Title = "Кобзар", Author = "Тарас Шевченко", Genre = "Поезія", PublicationYear = 1840, Description = "Збірка поетичних творів." },
                new Book { Title = "Тіні забутих предків", Author = "Михайло Коцюбинський", Genre = "Повість", PublicationYear = 1911, Description = "Класика української літератури." },
                new Book { Title = "1984", Author = "Джордж Орвелл", Genre = "Антиутопія", PublicationYear = 1949, Description = "Роман-антиутопія про тоталітарне суспільство." }
            );
            await db.SaveChangesAsync();
        }
    }
}
