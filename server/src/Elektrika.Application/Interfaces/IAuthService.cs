using Elektrika.Application.DTOs;

namespace Elektrika.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResultDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}
