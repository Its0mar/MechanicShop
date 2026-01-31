using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Identity;

public static class RefreshTokenErrors
{
    public static readonly Error IdRequired =
        Error.Validation("RefreshToken_Id_Required", "Refresh token ID is required.");

    public static readonly Error TokenRequired =
        Error.Validation("RefreshToken_Token_Required", "Token value is required.");

    public static readonly Error UserIdRequired =
        Error.Validation("RefreshToken_UserId_Required", "User ID is required.");

    public static readonly Error ExpiryInvalid =
        Error.Validation("RefreshToken_Expiry_Invalid", "Expiry must be in the future.");
}