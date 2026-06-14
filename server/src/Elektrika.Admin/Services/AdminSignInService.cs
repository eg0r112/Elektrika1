using System.Security.Claims;
using Elektrika.Domain.Entities;
using Elektrika.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elektrika.Admin.Services;

public sealed class AdminSignInService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public AdminSignInService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SignInAsync(HttpContext httpContext, string username, string password)
    {
        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == username.Trim());

        if (user is null)
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            });

        return true;
    }

    public Task SignOutAsync(HttpContext httpContext) =>
        httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
