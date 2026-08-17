using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Common;

/// <summary>
/// يضمن وجود مستخدم بنوع كابتن + صف في جدول Drivers.
/// </summary>
public static class DriverProvisioning
{
    public static async Task<Driver> EnsureDriverAsync(
        IAppDbContext db,
        string phone,
        string? fullName = null,
        string? email = null,
        Gender? gender = null,
        bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AppException("رقم الهاتف مطلوب");
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Phone == normalized && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Phone = normalized,
                FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Gender = gender,
                UserType = UserType.Driver,
                IsActive = true
            };
            db.Add(user);
            db.Add(new Wallet { UserId = user.Id, Balance = 0 });
        }
        else
        {
            if (user.UserType != UserType.Driver && user.UserType != UserType.Admin)
            {
                user.UserType = UserType.Driver;
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                user.FullName = fullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                user.Email = email.Trim();
            }

            if (gender is not null)
            {
                user.Gender = gender;
            }

            user.UpdatedAt = DateTime.UtcNow;
            db.Update(user);
        }

        var driver = await db.Drivers
            .FirstOrDefaultAsync(d => d.UserId == user.Id && !d.IsDeleted, cancellationToken);

        if (driver is null)
        {
            driver = new Driver
            {
                UserId = user.Id,
                IsActive = isActive
            };
            db.Add(driver);
        }

        return driver;
    }

    public static bool IsDriverRegistration(string? userType, string? deviceId, string? client = null)
    {
        if (!string.IsNullOrWhiteSpace(userType)
            && userType.Trim().Equals("driver", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(client)
            && (client.Trim().Equals("driver", StringComparison.OrdinalIgnoreCase)
                || client.Trim().Equals("captain", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // تطبيق الكباتن يرسل deviceId مع التسجيل؛ تطبيق الركاب لا يرسله.
        return !string.IsNullOrWhiteSpace(deviceId);
    }
}
