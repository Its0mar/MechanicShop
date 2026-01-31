using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Identity;

public sealed class RefreshToken : AuditableEntity
{
    public string? Token { get; }
    public string? UserId { get; }
    public DateTimeOffset ExpiresOnUtc { get; }


    private RefreshToken(Guid id, string? token, string? userId, DateTimeOffset expiresOnUtc) : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(Guid id, string? token, string? userId, DateTimeOffset expiresOnUtc)
    {
        if (id == Guid.Empty)
        {
            return RefreshTokenErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RefreshTokenErrors.UserIdRequired;
        }

        if (expiresOnUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(id, token, userId, expiresOnUtc);
    }
}