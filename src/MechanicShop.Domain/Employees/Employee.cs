using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;

namespace MechanicShop.Domain.Employees;

public sealed class Employee : AuditableEntity
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public Role Role { get; init; }
    public string FullName => $"{FirstName} {LastName}";

    private Employee()
    { }

    private Employee(Guid id) : base(id)
    { }


    public static Result<Employee> Create(Guid id, string firstName, string lastName, Role role)
    {
        if (id == Guid.Empty)
        {
            return EmployeeErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return EmployeeErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return EmployeeErrors.LastNameRequired;
        }

        if (!Enum.IsDefined(role))
        {
            return EmployeeErrors.RoleInvalid;
        }

        return new Employee(id)
        {
            FirstName = firstName,
            LastName = lastName,
            Role = role
        };
    }

}