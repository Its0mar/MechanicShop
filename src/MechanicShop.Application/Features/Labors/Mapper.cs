using MechanicShop.Domain.Employees;

namespace MechanicShop.Application.Features.Labors;

public static class LaborMapper
{
    public static LaborDto ToDto(this Employee employee)
    {
        return new LaborDto {LabordId = employee.Id, Name = employee.FullName};
    }

    public static List<LaborDto> ToDtos(this IEnumerable<Employee> employees)
    {
        return [.. employees.Select(e => e.ToDto())];
    }
}