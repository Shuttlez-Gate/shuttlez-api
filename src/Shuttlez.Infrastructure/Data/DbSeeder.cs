using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Infrastructure.Data;

public static class DbSeeder
{
    /// <summary>
    /// يضمن وجود حساب مسؤول لكل رقم في <c>Admin:BootstrapPhones</c>؛ يرقّي الحساب
    /// الموجود بدلاً من تكرار الرقم. الدخول للوحة التحكم يتم برمز OTP.
    /// </summary>
    public static async Task SeedAdminsAsync(AppDbContext db, IEnumerable<string> phones)
    {
        var normalized = phones
            .Select(PhoneNormalizer.Normalize)
            .Where(phone => !string.IsNullOrWhiteSpace(phone))
            .Distinct()
            .ToList();

        if (normalized.Count == 0) return;

        var existing = await db.UsersSet
            .Where(u => normalized.Contains(u.Phone))
            .ToListAsync();

        var changed = false;

        foreach (var user in existing)
        {
            if (user.UserType == UserType.Admin && user.IsActive && !user.IsDeleted) continue;

            user.UserType = UserType.Admin;
            user.IsActive = true;
            user.IsDeleted = false;
            user.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        foreach (var phone in normalized.Except(existing.Select(u => u.Phone)))
        {
            var admin = new User
            {
                Phone = phone,
                FullName = "مسؤول النظام",
                UserType = UserType.Admin,
                IsActive = true
            };
            db.UsersSet.Add(admin);
            db.WalletsSet.Add(new Wallet { UserId = admin.Id, Balance = 0 });
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedFaqAsync(db);
        await SeedLegalDocumentsAsync(db);
        await SeedSubscriptionPackagesAsync(db);
        await SyncCaptainLeadsToDriversAsync(db);

        if (!await db.Routes.AnyAsync())
        {
            var shoroukToNasr = new Route
            {
                Name = "الشروق - مدينة نصر",
                Description = "طريق السويس، مدينة الشروق - القاهرة",
                StartLatitude = 30.1219,
                StartLongitude = 31.4956,
                EndLatitude = 30.0561,
                EndLongitude = 31.3302,
                IsActive = true
            };

            var shoroukToHelwan = new Route
            {
                Name = "الشروق - حلوان",
                Description = "طريق السويس، مدينة الشروق - القاهرة",
                StartLatitude = 30.1219,
                StartLongitude = 31.4956,
                EndLatitude = 29.8492,
                EndLongitude = 31.3342,
                IsActive = true
            };

            db.RoutesSet.AddRange(shoroukToNasr, shoroukToHelwan);

            db.StopsSet.AddRange(
                new Stop
                {
                    Route = shoroukToNasr,
                    Name = "شارع الطيران",
                    Latitude = 30.115,
                    Longitude = 31.48,
                    Order = 1
                },
                new Stop
                {
                    Route = shoroukToNasr,
                    Name = "طيبة مول",
                    Latitude = 30.09,
                    Longitude = 31.42,
                    Order = 2
                },
                new Stop
                {
                    Route = shoroukToNasr,
                    Name = "الستاد، يوسف عباس",
                    Latitude = 30.0561,
                    Longitude = 31.3302,
                    Order = 3
                });

            var miniBus = new Vehicle
            {
                PlateNumber = "م د 123",
                Model = "Toyota HiAce",
                Type = VehicleType.MiniBus,
                Capacity = 13,
                IsActive = true
            };

            var car = new Vehicle
            {
                PlateNumber = "س ي 789",
                Model = "Hyundai Elantra",
                Type = VehicleType.CarShuttle,
                Capacity = 4,
                IsActive = true
            };

            var bus = new Vehicle
            {
                PlateNumber = "أ ب ج 456",
                Model = "Mercedes Sprinter",
                Type = VehicleType.Bus,
                Capacity = 24,
                IsActive = true
            };

            db.VehiclesSet.AddRange(miniBus, car, bus);

            var driverUser = new User
            {
                Phone = "01099999999",
                FullName = "محمد رضا السيد",
                AvatarUrl = "https://i.pravatar.cc/96?img=12",
                UserType = UserType.Driver,
                Gender = Gender.Male,
                IsActive = true
            };

            db.UsersSet.Add(driverUser);

            var driver = new Driver
            {
                User = driverUser,
                Vehicle = miniBus,
                RatingAverage = 4.8m,
                RatingCount = 120,
                IsOnline = true,
                IsActive = true
            };

            db.DriversSet.Add(driver);

            var today = DateTime.UtcNow.Date;
            var tripHours = new[] { 5, 7, 9 };
            var tripIndex = 0;

            for (var dayOffset = 0; dayOffset < 10; dayOffset++)
            {
                var day = today.AddDays(dayOffset);
                if (day.DayOfWeek == DayOfWeek.Friday) continue;

                foreach (var hour in tripHours)
                {
                    var route = tripIndex % 2 == 0 ? shoroukToNasr : shoroukToHelwan;

                    db.TripsSet.Add(new Trip
                    {
                        Route = route,
                        Driver = driver,
                        Status = TripStatus.Scheduled,
                        ScheduledAt = day.AddHours(hour),
                        PricePerSeat = 100m,
                        AvailableSeats = miniBus.Capacity,
                        ReferenceCode = $"#TR{day:yy}-{1000 + tripIndex}"
                    });

                    tripIndex++;
                    if (tripIndex >= 18) break;
                }

                if (tripIndex >= 18) break;
            }
        }

        await NormalizeMiniBusCapacitiesAsync(db);
        await SeedLandingCatalogRoutesAsync(db);
        await db.SaveChangesAsync();
        await SeedDriverReviewsAsync(db);
        await SeedSampleNotificationsAsync(db);
        await SeedSupportTicketsAsync(db);
    }

    private static async Task SeedDriverReviewsAsync(AppDbContext db)
    {
        if (await db.ReviewsSet.AnyAsync())
            return;

        var driver = await db.DriversSet
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.User.Phone == "01099999999" && !d.IsDeleted);

        if (driver is null)
            return;

        var passenger = await db.UsersSet
            .FirstOrDefaultAsync(u => u.UserType == UserType.Passenger && u.IsActive && !u.IsDeleted);

        if (passenger is null)
        {
            passenger = new User
            {
                Phone = "01011111111",
                FullName = "أحمد محمد",
                UserType = UserType.Passenger,
                IsActive = true,
            };
            db.UsersSet.Add(passenger);
            db.WalletsSet.Add(new Wallet { UserId = passenger.Id, Balance = 0 });
            await db.SaveChangesAsync();
        }

        var trips = await db.TripsSet
            .Where(t => t.DriverId == driver.Id && !t.IsDeleted)
            .OrderBy(t => t.ScheduledAt)
            .Take(8)
            .ToListAsync();

        if (trips.Count == 0)
            return;

        var stars = new[] { 5, 4, 5, 4, 5, 3, 5, 4 };
        var comments = new[]
        {
            "التزام بالمواعيد و تعامل راقي جدًا",
            "رحلة أمنة و مكيفة، الكابتن ذوق و معاملة محترمة",
            "تجربة ممتازة، أنصح بالتعامل معاه",
            "رحلة مريحة ووصلنا في المعاد",
            "كابتن محترم وقيادة آمنة",
            "تجربة جيدة بشكل عام",
            "خدمة ممتازة ونظافة عالية",
            "تعامل راقي ورحلة هادئة",
        };

        for (var i = 0; i < trips.Count; i++)
        {
            trips[i].Status = TripStatus.Completed;

            db.ReviewsSet.Add(new Review
            {
                TripId = trips[i].Id,
                UserId = passenger.Id,
                DriverId = driver.Id,
                Stars = stars[i % stars.Length],
                Comment = comments[i % comments.Length],
                CreatedAt = trips[i].ScheduledAt.AddHours(2),
            });
        }

        driver.RatingAverage = 4.5m;
        driver.RatingCount = trips.Count;

        await db.SaveChangesAsync();
    }

    /// <summary>يضيف مسارات العرض في صفحة الهبوط إن لم تكن موجودة.</summary>
    private static async Task SeedLandingCatalogRoutesAsync(AppDbContext db)
    {
        foreach (var item in LandingCatalogSeedData.Routes)
        {
            if (await db.RoutesSet.AnyAsync(r => r.Name == item.Name))
                continue;

            var route = new Route
            {
                Name = item.Name,
                Description = item.Description,
                StartLatitude = item.StartLat,
                StartLongitude = item.StartLng,
                EndLatitude = item.EndLat,
                EndLongitude = item.EndLng,
                IsActive = true,
            };

            db.RoutesSet.Add(route);

            foreach (var stop in item.Stops)
            {
                db.StopsSet.Add(new Stop
                {
                    Route = route,
                    Name = stop.Name,
                    Latitude = stop.Lat,
                    Longitude = stop.Lng,
                    Order = stop.Order,
                });
            }
        }
    }

    /// يصحّح سعة الميني باص والمقاعد المتاحة في بيانات التطوير القديمة.
    private static async Task NormalizeMiniBusCapacitiesAsync(AppDbContext db)
    {
        const int miniBusCapacity = 13;

        var miniBuses = await db.VehiclesSet
            .Where(v => v.Type == VehicleType.MiniBus && v.Capacity != miniBusCapacity)
            .ToListAsync();

        foreach (var vehicle in miniBuses)
            vehicle.Capacity = miniBusCapacity;

        if (miniBuses.Count == 0) return;

        var miniBusIds = miniBuses.Select(v => v.Id).ToList();

        var driverIds = await db.DriversSet
            .Where(d => d.VehicleId != null && miniBusIds.Contains(d.VehicleId.Value))
            .Select(d => d.Id)
            .ToListAsync();

        if (driverIds.Count == 0) return;

        var trips = await db.TripsSet
            .Where(t => t.DriverId != null && driverIds.Contains(t.DriverId.Value) && t.AvailableSeats > miniBusCapacity)
            .ToListAsync();

        foreach (var trip in trips)
            trip.AvailableSeats = miniBusCapacity;
    }

    private static async Task SeedSubscriptionPackagesAsync(AppDbContext db)
    {
        if (await db.SubscriptionPackagesSet.AnyAsync())
            return;

        db.SubscriptionPackagesSet.AddRange(
            new SubscriptionPackage
            {
                Name = "باقة 10 رحلة",
                Description = "لمدة 30 يوم;oldPrice:1000",
                Price = 800,
                TripCount = 10,
                ValidityDays = 30,
                IsActive = true,
            },
            new SubscriptionPackage
            {
                Name = "باقة 20 رحلة",
                Description = "لمدة 30 يوم;oldPrice:1500",
                Price = 1200,
                TripCount = 20,
                ValidityDays = 30,
                IsActive = true,
            },
            new SubscriptionPackage
            {
                Name = "باقة 30 رحلة",
                Description = "لمدة 30 يوم;oldPrice:2500",
                Price = 2000,
                TripCount = 30,
                ValidityDays = 30,
                IsActive = true,
            },
            new SubscriptionPackage
            {
                Name = "باقة غير محدودة",
                Description = "لمدة 30 يوم;oldPrice:2800",
                Price = 2200,
                TripCount = 0,
                ValidityDays = 30,
                IsActive = true,
            });

        await db.SaveChangesAsync();
    }

    private static async Task SeedSampleNotificationsAsync(AppDbContext db)
    {
        if (await db.NotificationsSet.AnyAsync())
            return;

        var passenger = await db.UsersSet
            .Where(u => u.UserType == UserType.Passenger && u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        if (passenger is null)
            return;

        var now = DateTime.UtcNow;
        db.NotificationsSet.AddRange(
            new Notification
            {
                UserId = passenger.Id,
                Title = "مرحباً بك في Shuttlez",
                Body = "يمكنك الآن حجز رحلاتك والاشتراك في الباقات المتاحة.",
                Type = "welcome",
                IsRead = false,
                CreatedAt = now.AddHours(-2),
            },
            new Notification
            {
                UserId = passenger.Id,
                Title = "رحلة جديدة",
                Body = "تذكير: يمكنك متابعة رحلاتك من قسم الرحلات.",
                Type = "trip",
                IsRead = true,
                CreatedAt = now.AddDays(-1),
                ReadAt = now.AddDays(-1).AddHours(1),
            });

        await db.SaveChangesAsync();
    }

    private static async Task SeedSupportTicketsAsync(AppDbContext db)
    {
        if (await db.SupportTicketsSet.AnyAsync())
            return;

        var passenger = await db.UsersSet
            .Where(u => u.UserType == UserType.Passenger && u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        if (passenger is null)
            return;

        var booking = await db.BookingsSet
            .Include(b => b.Trip)
            .Where(b => b.UserId == passenger.Id && b.Status != BookingStatus.Cancelled)
            .OrderByDescending(b => b.Trip.ScheduledAt)
            .FirstOrDefaultAsync();

        if (booking?.Trip is null)
            return;

        var openTicket = new SupportTicket
        {
            UserId = passenger.Id,
            TripId = booking.TripId,
            Subject = booking.Trip.ReferenceCode ?? "بلاغ دعم فني",
            Status = "open",
        };

        db.SupportTicketsSet.Add(openTicket);
        db.SupportMessagesSet.AddRange(
            new SupportMessage
            {
                Ticket = openTicket,
                SenderId = passenger.Id,
                IsFromSupport = false,
                Content = "مرحباً، أحتاج مساعدة بخصوص رحلتي.",
            },
            new SupportMessage
            {
                Ticket = openTicket,
                SenderId = passenger.Id,
                IsFromSupport = true,
                Content = "مرحباً بك، فريق الدعم الفني جاهز لمساعدتك.",
            });

        var closedTicket = new SupportTicket
        {
            UserId = passenger.Id,
            TripId = booking.TripId,
            Subject = $"منتهي - {booking.Trip.ReferenceCode ?? "بلاغ"}",
            Status = "closed",
            ClosedAt = DateTime.UtcNow.AddDays(-2),
        };

        db.SupportTicketsSet.Add(closedTicket);
        db.SupportMessagesSet.Add(
            new SupportMessage
            {
                Ticket = closedTicket,
                SenderId = passenger.Id,
                IsFromSupport = true,
                Content = "تم إغلاق البلاغ. شكراً لتواصلك معنا.",
            });

        await db.SaveChangesAsync();
    }

    private static async Task SeedLegalDocumentsAsync(AppDbContext db)
    {
        var documents = new Dictionary<string, (string Title, string Content)>
        {
            ["terms"] = ("الشروط و الاحكام", ContentSeedData.TermsJson),
            ["privacy"] = ("سياسة الخصوصية", ContentSeedData.PrivacyJson),
            ["about"] = ("عن التطبيق", ContentSeedData.AboutJson),
        };

        foreach (var (slug, (title, content)) in documents)
        {
            var existing = await db.LegalDocuments
                .FirstOrDefaultAsync(d => d.Slug == slug);

            if (existing is null)
            {
                db.LegalDocumentsSet.Add(new LegalDocument
                {
                    Slug = slug,
                    Title = title,
                    Content = content,
                    IsActive = true
                });
            }
            else
            {
                existing.Title = title;
                existing.Content = content;
                existing.IsActive = true;
            }
        }
    }

    private static async Task SeedFaqAsync(AppDbContext db)
    {
        var expectedCount = ContentSeedData.FaqItems.Count;
        var currentCount = await db.FaqItems.CountAsync();

        if (currentCount == expectedCount)
        {
            return;
        }

        var existing = await db.FaqItems.ToListAsync();
        if (existing.Count > 0)
        {
            db.FaqItemsSet.RemoveRange(existing);
        }

        db.FaqItemsSet.AddRange(
            ContentSeedData.FaqItems.Select(item => new FaqItem
            {
                Question = item.Question,
                Answer = item.Answer,
                Order = item.Order
            }));
    }

    /// <summary>
    /// يحوّل طلبات الكباتن من الويب إلى حسابات Drivers لتظهر في لوحة التحكم.
    /// </summary>
    private static async Task SyncCaptainLeadsToDriversAsync(AppDbContext db)
    {
        var leads = await db.LandingCaptainLeadsSet
            .Where(l => !l.IsDeleted)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        if (leads.Count == 0) return;

        foreach (var lead in leads)
        {
            if (string.IsNullOrWhiteSpace(lead.Phone)) continue;

            var normalized = PhoneNormalizer.Normalize(lead.Phone);
            if (lead.Phone != normalized)
            {
                lead.Phone = normalized;
                lead.UpdatedAt = DateTime.UtcNow;
            }

            await DriverProvisioning.EnsureDriverAsync(
                db,
                normalized,
                lead.FullName,
                isActive: true);

            await db.SaveChangesAsync();
        }
    }
}
