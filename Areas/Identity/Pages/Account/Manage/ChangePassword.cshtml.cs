#nullable disable

using System.ComponentModel.DataAnnotations;
using BookTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookTracker.Areas.Identity.Pages.Account.Manage
{
    /// <summary>Зміна пароля авторизованим користувачем.</summary>
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Вкажіть поточний пароль")]
            [DataType(DataType.Password)]
            [Display(Name = "Поточний пароль")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "Вкажіть новий пароль")]
            [StringLength(100, ErrorMessage = "Пароль має містити від {2} до {1} символів", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Новий пароль")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Повторіть новий пароль")]
            [Compare("NewPassword", ErrorMessage = "Паролі не збігаються")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            if (!await _userManager.HasPasswordAsync(user))
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Пароль успішно змінено";

            return RedirectToPage();
        }
    }
}
