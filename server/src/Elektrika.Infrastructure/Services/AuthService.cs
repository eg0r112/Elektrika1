using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elektrika.Application.DTOs;
using Elektrika.Application.Interfaces;
using Elektrika.Application.Options;
using Elektrika.Domain.Entities;
using Elektrika.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Elektrika.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public AuthService(AppDbContext context, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResultDto?> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return null;
        }

        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(
                u => u.Username == dto.Username.Trim(),
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new LoginResultDto
        {
            Token = GenerateToken(user),
            Username = user.Username,
        };
    }

    private string GenerateToken(AdminUser user)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var expires = DateTime.UtcNow.AddMinutes(
            _jwtOptions.ExpirationMinutes > 0 ? _jwtOptions.ExpirationMinutes : 60);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(_jwtOptions.Issuer) ? null : _jwtOptions.Issuer,
            audience: string.IsNullOrWhiteSpace(_jwtOptions.Audience) ? null : _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
