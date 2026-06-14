namespace Elektrika.Application.DTOs;

public sealed class LoginDto
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
