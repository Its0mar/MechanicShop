using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Customers.Queries;

public sealed class GetCustomersQuery : ICachedQuery<Result<List<CustomerDto>>>
{
    public string CacheKey => "customers";

    public string[] Tags => ["customers"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

}

public class GetCustomersQueryHandler(IAppDbContext appDbContext) : IRequestHandler<GetCustomersQuery, Result<List<CustomerDto>>>
{
    private readonly IAppDbContext _appDbContext = appDbContext;

    public async Task<Result<List<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var customers = await _appDbContext.Customers.Include(c => c.Vehicles).AsNoTracking().ToListAsync();
        return customers.ToDtos();
    }
}