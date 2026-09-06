using Microsoft.AspNetCore.Identity;

namespace BookTracker.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
