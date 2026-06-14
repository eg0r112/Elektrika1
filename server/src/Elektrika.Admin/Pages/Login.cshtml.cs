using Elektrika.Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elektrika.Admin.Pages;

public sealed class LoginModel : PageModel
{
    private readonly AdminSignInService _signInService;

    public LoginModel(AdminSignInService signInService)
    {
        _signInService = signInService;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var success = await _signInService.SignInAsync(HttpContext, Username, Password);
        if (!success)
        {
            ErrorMessage = "Неверный логин или пароль.";
            return Page();
        }

        return RedirectToPage("/Index");
    }
}
