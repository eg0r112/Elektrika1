namespace Elektrika.Application.DTOs;

public sealed class LoginResultDto
{
    public string Token { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;
}
