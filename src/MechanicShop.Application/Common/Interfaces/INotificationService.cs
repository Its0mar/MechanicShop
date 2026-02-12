namespace MechanicShop.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string email, CancellationToken ct = default);
    Task SendSmsAsync(string phone, CancellationToken ct = default);
}