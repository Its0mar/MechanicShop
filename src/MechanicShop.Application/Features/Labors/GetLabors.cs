using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Labors;

public sealed record GetLaborsQuery() : ICachedQuery<Result<List<LabordDto>>>
{
    public string CacheKey => "labors";

    public string[] Tags => ["labors"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}

public class GetLaborsQueryHandlder(IAppDbContext context) : IRequestHandler<GetLaborsQuery, Result<List<LabordDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<LabordDto>>> Handle(GetLaborsQuery query, CancellationToken ct)
    {
        var labors = await _context.Employees.AsNoTracking().Where(e => e.Role == Role.Labor).ToListAsync(ct);

        return labors.ToDtos();
    }
}