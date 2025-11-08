using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Customers;

public static class CustomerErrors
{
    public static Error NameRequired =>
        Error.Validation("Customer_Name_Required", "Customer name is required");

    public static Error PhoneNumberRequired =>
        Error.Validation("Customer_Number_Required", "Phone number is required");

    public static Error EmailRequired =>
        Error.Validation("Customer_Email_Required", "Email is required");

    public static Error EmailInvalid =>
        Error.Validation("Customer_Email_Invalid", "Email is invalid");

    public static Error CustomerExists =>
        Error.Conflict("Customer_Email_Exists", "A customer with this email already exists.");

    public static Error InvalidPhoneNumber =>
        Error.Conflict("Customer.InvalidPhoneNumber", "Phone number must be 7–15 digits and may start with '+'.");

    public static Error CannotDeleteCustomerWithWorkOrders =>
        Error.Conflict("Customer.CannotDelete", "Customer cannot be deleted due to existing work orders.");
}