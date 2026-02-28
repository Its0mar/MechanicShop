using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Labors;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Application.Features.Scheduling.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.WorkOrders.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Scheduling.Queries;

public sealed record GetDailyScheduleQuery(
    TimeZoneInfo TimeZone,
    DateOnly ScheduleDate,
    Guid? LaborId = null) : ICachedQuery<Result<ScheduleDto>>
{
    public string CacheKey => $"work-order:{ScheduleDate:yyyy-MM-dd}:labor={LaborId?.ToString() ?? "-"}";
    public string[] Tags => ["work-order"];
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}

public class GetDailyScheduleQueryHandler(
    IAppDbContext context,
    TimeProvider datetime)
    : IRequestHandler<GetDailyScheduleQuery, Result<ScheduleDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _datetime = datetime;

    public async Task<Result<ScheduleDto>> Handle(GetDailyScheduleQuery query, CancellationToken ct)
    {
        var localStart = query.ScheduleDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = localStart.AddDays(1);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, query.TimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, query.TimeZone);

        var workOrders = await _context.WorkOrders
            .Where(w =>
                w.StartAtUtc < utcEnd &&
                w.EndAtUtc > utcStart &&
                (query.LaborId == null || w.LaborId == query.LaborId))
            .Include(w => w.RepairTasks)
            .Include(w => w.Vehicle)
            .Include(w => w.Labor)
            .ToListAsync(ct);

        var now = TimeZoneInfo.ConvertTime(_datetime.GetUtcNow(), query.TimeZone);

        var result = new ScheduleDto
        {
            OnDate = query.ScheduleDate,
            EndOfDay = localEnd < now,
            Spots = []
        };

        foreach (var spot in Enum.GetValues<Spot>())
        {
            var current = localStart;
            var slots = new List<AvailabilitySlotDto>();

            var woBySpot = workOrders
                .Where(w => w.Spot == spot)
                .OrderBy(w => w.StartAtUtc)
                .ToList();

            while (current < localEnd)
            {
                var next = current.AddMinutes(15);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(current, query.TimeZone);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(next, query.TimeZone);

                var wo = woBySpot.FirstOrDefault(w =>
                    w.StartAtUtc < endUtc && w.EndAtUtc > startUtc);

                if (wo != null)
                {
                    if (!slots.Any(s => s.WorkOrderId == wo.Id))
                    {
                        slots.Add(new AvailabilitySlotDto
                        {
                            WorkOrderId = wo.Id,
                            Spot = spot,
                            StartAt = wo.StartAtUtc,
                            EndAt = wo.EndAtUtc,
                            Vehicle = FormatVehicleInfo(wo.Vehicle!),
                            Labor = wo.Labor!.ToDto(),
                            IsOccupied = true,
                            RepairTasks = [.. wo.RepairTasks.ToList().ConvertAll(rt => rt.ToDto())],
                            WorkOrderLocked = !wo.IsEditable,
                            State = wo.State,
                            IsAvailable = false
                        });
                    }
                }
                else
                {
                    slots.Add(new AvailabilitySlotDto
                    {
                        Spot = spot,
                        StartAt = startUtc,
                        EndAt = endUtc,
                        WorkOrderLocked = false,
                        IsAvailable = current >= now
                    });
                }

                current = next;
            }

            result.Spots.Add(new SpotDto
            {
                Spot = spot,
                Slots = slots
            });
        }

        return result;
    }

    private static string? FormatVehicleInfo(Vehicle vehicle) =>
        vehicle != null ? $"{vehicle.Make} | {vehicle.LicensePlate}" : null;
}