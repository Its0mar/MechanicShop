using System.Security;
using System.Security.Claims;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Queries.RefreshTokens;

public class RefreshTokenHandler(IIdentityService identityService,
                                 ILogger<RefreshTokenHandler> logger,
                                 ITokenProvider tokenProvider,
                                 IAppDbContext context) : IRequestHandler<RefreshTokenQuery, Result<TokenResponse>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<RefreshTokenHandler> _logger = logger;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IAppDbContext _context = context;

    public async Task<Result<TokenResponse>> Handle(RefreshTokenQuery request, CancellationToken ct)
    {
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);

        if (principal is null)
        {
            _logger.LogError("Expired access token is not valid");
            return ApplicationErrors.ExpiredAccessTokenInvalid;                
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            _logger.LogError("Invalid userId claim");
            return ApplicationErrors.UserIdClaimInvalid;
        }

        var getUserResult  = await _identityService.GetUserByIdAsync(userId);

        if (getUserResult.IsError)
        {
            _logger.LogError("Get user by id error occurred: {ErrorDescription}", getUserResult.TopError.Description);
            return getUserResult.Errors ?? [];
        }

        var refreshToekn = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == request.RefreshToken , ct);

        if (refreshToekn is null || refreshToekn.ExpiresOnUtc < DateTime.UtcNow)
        {
            _logger.LogError("Refresh token has expired");
            return ApplicationErrors.RefreshTokenExpired;
        }

        var generateTokenResult = await _tokenProvider.GenerateJwtTokenAsync(getUserResult.Value, ct);

        if (generateTokenResult.IsError)
        {
            _logger.LogError("Generate token error occurred: {ErrorDescription}", generateTokenResult.TopError.Description);

            return generateTokenResult.Errors ?? [];
        }
        return generateTokenResult.Value;
    }
}