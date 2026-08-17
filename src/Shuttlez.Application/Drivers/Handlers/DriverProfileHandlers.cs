using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Drivers.DTOs;
using Shuttlez.Application.Drivers.Queries;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Drivers.Handlers;

public class DriverProfileHandlers :
    IRequestHandler<GetMyDriverProfileQuery, DriverProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DriverProfileHandlers(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DriverProfileDto> Handle(
        GetMyDriverProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        if (!string.Equals(_currentUser.Role, UserType.Driver.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAppException("هذا الإجراء متاح للكباتن فقط");

        var driver = await _db.Drivers
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        var user = driver.User;
        var vehicle = driver.Vehicle;

        return new DriverProfileDto(
            user.Id,
            user.Phone,
            user.FullName,
            user.Email,
            user.Gender switch
            {
                Gender.Male => "ذكر",
                Gender.Female => "أنثى",
                _ => null
            },
            user.AvatarUrl,
            (double)driver.RatingAverage,
            driver.RatingCount,
            vehicle is null ? null : VehicleTypeLabel(vehicle.Type),
            vehicle?.Model,
            vehicle?.PlateNumber,
            vehicle?.Capacity);
    }

    private static string VehicleTypeLabel(VehicleType type) => type switch
    {
        VehicleType.CarShuttle => "عربية شاتيل",
        VehicleType.Bus => "اتوبيس شاتيل",
        _ => "ميني باص"
    };
}
