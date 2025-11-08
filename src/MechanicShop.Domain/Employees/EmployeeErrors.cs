using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Employees;

public static class EmployeeErrors
{
    public static readonly Error IdRequired =
        Error.Validation("Employee.Id.Required", "Employee Id is required.");

    public static Error FirstNameRequired =>
        Error.Validation("Employee.FirstName.Required", "First name is required.");

    public static Error LastNameRequired =>
        Error.Validation("Employee.LastName.Required", "Last name is required.");

    public static Error RoleInvalid =>
        Error.Validation("Employee.Role.Invalid", "Invalid role assigned to employee.");
}