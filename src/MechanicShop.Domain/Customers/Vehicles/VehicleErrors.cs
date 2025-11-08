using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Customers.Vehicles;

public static class VehicleErrors
{
    public static Error MakeRequired
        => Error.Validation("Vehicle_Make_Required", "Vehicle make is required");

    public static Error ModelRequired
        => Error.Validation("Vehicle_Model_Required", "Vehicle model is required");

    public static Error LicensePlateRequired
        => Error.Validation("Vehicle_LicensePlate_Make_Required", "Vehicle license plate is required");

    public static Error YearInvalid
        => Error.Validation("Vehicle_Year_Invalid", "Year must be between 1886 and next year.");
}