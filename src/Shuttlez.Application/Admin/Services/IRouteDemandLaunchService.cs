using Shuttlez.Application.Admin.DTOs;

namespace Shuttlez.Application.Admin.Services;

/// <summary>
/// Admin-controlled operational launch: READY demand corridor → single Trip.
/// Does not convert demand to bookings. Does not invent Group entities.
/// </summary>
public interface IRouteDemandLaunchService
{
    Task<RouteDemandLaunchResultDto> LaunchAsync(
        string routeKey,
        LaunchRouteDemandRequest request,
        Guid? adminUserId,
        CancellationToken cancellationToken = default);
}
