using System.Security.Claims;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Infrastructure.Identity.Policies;

public class LaborAssignedRequirement : IAuthorizationRequirement;

public class LaborAssignedHandler(IAppDbContext context,
                                  IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<LaborAssignedRequirement>
{
    private readonly IAppDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LaborAssignedRequirement requirement)
    {
        //Only the assigned labor OR a manager can access this work order.
        
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        // Extract WorkOrderId dynamically from the route
        var workOrderIdString = _httpContextAccessor.HttpContext?.Request.RouteValues["WorkOrderId"]?.ToString();
        if (!Guid.TryParse(workOrderIdString, out var workOrderId))
        {
            context.Fail();
            return;
        }

        var isAssigned = await _context.WorkOrders
            .AnyAsync(a => a.Id == workOrderId && a.LaborId == Guid.Parse(userId));
        
        if (isAssigned)
        {
            context.Succeed(requirement);
            return;
        }
        
        if (context.User.IsInRole(nameof(Role.Manager)))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}