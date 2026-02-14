using MechanicShop.Domain.Employees;

namespace MechanicShop.Application.Features.Labors;

public static class LaborMapper
{
    public static LabordDto ToDto(this Employee employee)
    {
        return new LabordDto {LabordId = employee.Id, Name = employee.FullName};
    }

    public static List<LabordDto> ToDtos(this IEnumerable<Employee> employees)
    {
        return [.. employees.Select(e => e.ToDto())];
    }
}