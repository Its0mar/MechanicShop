using System.Net.Mail;
using System.Text.RegularExpressions;
using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers.Vehicles;

namespace MechanicShop.Domain.Customers;

public sealed class Customer : AuditableEntity
{
    public string? Name { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }

    private readonly List<Vehicle> _vehicles = [];
    public IEnumerable<Vehicle> Vehicles => _vehicles.AsReadOnly();

    private Customer()
    { }

    private Customer(Guid id, string name, string phoneNumber, string email, List<Vehicle> vehicles) 
     : base(id)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;
        _vehicles = [.. vehicles]; 
    }

    public static Result<Customer> Create(Guid id, string name, string phoneNumber, string email, List<Vehicle> vehicles)
    {
        var error = Validate(name, phoneNumber, email);

        if (error is not null)
        {
            return error;
        }

        return new Customer(id, name, phoneNumber, email, vehicles);
    }

    public Result<Updated> Update(string name, string phoneNumber, string email)
    {
        var error = Validate(name, phoneNumber, email);

        if (error is not null)
        {
            return error;
        }

        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;

        return Result.Updated;
    }

    public Result<Updated> UpsertVehicles(List<Vehicle> incomingVehicles)
    {
        _vehicles.RemoveAll(existing => incomingVehicles.Any(inc => inc.Id == existing.Id));

        foreach (var vehicle in incomingVehicles)
        {
            var existing = _vehicles.FirstOrDefault(v => v.Id == vehicle.Id);

            if (existing is null)
            {
                _vehicles.Add(vehicle);
            }

            else
            {
                var updateVehicleResult = existing.Update(vehicle.Make, vehicle.Model, vehicle.Year, vehicle.LicensePlate);

                if (updateVehicleResult.IsError)
                {
                    return updateVehicleResult.Errors ?? [];
                }
            }
        }

        return Result.Updated;
    }


    #region CommonValidation

    private static Error? Validate(string name, string phoneNumber, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CustomerErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return CustomerErrors.PhoneNumberRequired;
        }

        if (!Regex.IsMatch(phoneNumber, @"^\+?\d{7,15}$"))
        {
            return CustomerErrors.InvalidPhoneNumber;
        }
                if (string.IsNullOrWhiteSpace(email))
        {
            return CustomerErrors.EmailRequired;
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch
        {
            return CustomerErrors.EmailInvalid;
        }

        return null;
    }

    #endregion
}