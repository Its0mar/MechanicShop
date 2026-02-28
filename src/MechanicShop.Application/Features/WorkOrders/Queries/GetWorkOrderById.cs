using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Mappers;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Queries;

public sealed record GetWorkOrderByIdQuery(Guid WorkOrderId) : ICachedQuery<Result<WorkOrderDto>>
{
    public string CacheKey => $"work-order:{WorkOrderId}";
    public string[] Tags => ["work-order"];
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}


public sealed class GetWorkOrderByIdQueryValidator : AbstractValidator<GetWorkOrderByIdQuery>
{
    public GetWorkOrderByIdQueryValidator()
    {
        RuleFor(wo => wo.WorkOrderId)
            .NotEmpty()
            .WithErrorCode("WorkOrderId_Is_Required")
            .WithMessage("WorkOrderId is required.");
    }
}


public class GetWorkOrderByIdQueryHandler(
    ILogger<GetWorkOrderByIdQueryHandler> logger,
    IAppDbContext context
    )
    : IRequestHandler<GetWorkOrderByIdQuery, Result<WorkOrderDto>>
{
    private readonly ILogger<GetWorkOrderByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<WorkOrderDto>> Handle(GetWorkOrderByIdQuery query, CancellationToken ct)
    {
        var workOrder = await _context.WorkOrders.AsNoTracking()
                                            .Include(a => a.RepairTasks)
                                               .ThenInclude(a => a.Parts)
                                            .Include(a => a.Labor)
                                            .Include(a => a.Vehicle!)
                                                .ThenInclude(v => v.Customer)
                                            .Include(a => a.Invoice)
                                        .FirstOrDefaultAsync(a => a.Id == query.WorkOrderId, ct);

        if (workOrder is null)
        {
            _logger.LogWarning("WorkOrder with id {WorkOrderId} was not found", query.WorkOrderId);

            return ApplicationErrors.WorkOrderNotFound;
        }

        return workOrder.ToDto();
    }
}