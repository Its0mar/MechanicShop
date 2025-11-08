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

    private Customer(Guid id, string name, string phoneNumber, string email, List<Vehicle> vehicles) : base(id)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;
        _vehicles = vehicles;
    }

    public static Result<Customer> Create(Guid id, string name, string phoneNumber, string email, List<Vehicle> vehicles)
    {
        var validationResult = Validate(name, phoneNumber, email);
        if (validationResult is not null) return validationResult;

        return new Customer(id, name, phoneNumber, email, vehicles);
    }

    public Result<Updated> Update(string name, string phoneNumber, string email)
    {
        var validationResult = Validate(name, phoneNumber, email);
        if (validationResult is not null) return validationResult;

        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;

        return Result.Updated;
    }

    public Result<Updated> UpsertParts(List<Vehicle> incomingVehicle)
    {
        _vehicles.RemoveAll(existing => incomingVehicle.All(v => v.Id != existing.Id));

        foreach (var incoming in incomingVehicle)
        {
            var existing = _vehicles.FirstOrDefault(v => v.Id == incoming.Id);

            if (existing is null)
                _vehicles.Add(incoming);
                
            else
            {
                var updateResult = existing.Update(
                    incoming.Make, incoming.Model, incoming.Year, incoming.LicensePlate);

                if (updateResult.IsError)
                {
                    return updateResult.Errors;
                }

            }    
        }
        
        return Result.Updated;
    }
    private static Error? Validate(string name, string phoneNumber, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CustomerErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber) || !Regex.IsMatch(phoneNumber, @"^\+?\d{7,15}$"))
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



}