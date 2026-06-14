using Elektrika.Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elektrika.Admin.Pages;

public sealed class LogoutModel : PageModel
{
    private readonly AdminSignInService _signInService;

    public LogoutModel(AdminSignInService signInService)
    {
        _signInService = signInService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInService.SignOutAsync(HttpContext);
        return RedirectToPage("/Login");
    }
}
