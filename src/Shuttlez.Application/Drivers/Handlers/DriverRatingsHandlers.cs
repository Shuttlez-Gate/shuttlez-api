using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Drivers.DTOs;
using Shuttlez.Application.Drivers.Queries;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Drivers.Handlers;

public class DriverRatingsHandlers :
    IRequestHandler<GetMyDriverRatingsQuery, DriverRatingsSummaryDto>
{
    private static readonly (string Key, string Label)[] TagCatalog =
    [
        ("good", "جيد"),
        ("clean", "نظافة"),
        ("comfortable", "مريح"),
        ("honesty", "أمانة"),
        ("ac", "تكييف جيد"),
        ("punctual", "التزام"),
    ];

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DriverRatingsHandlers(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DriverRatingsSummaryDto> Handle(
        GetMyDriverRatingsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        if (!string.Equals(_currentUser.Role, UserType.Driver.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAppException("هذا الإجراء متاح للكباتن فقط");

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        var reviews = await _db.Reviews
            .Where(r => r.DriverId == driver.Id && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Stars,
                r.Comment,
                r.TripId,
                r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var totalTrips = await _db.Trips
            .CountAsync(
                t => t.DriverId == driver.Id &&
                     t.Status == TripStatus.Completed &&
                     !t.IsDeleted,
                cancellationToken);

        var breakdown = Enumerable.Range(1, 5).ToDictionary(stars => stars, _ => 0);
        foreach (var review in reviews)
        {
            if (review.Stars is >= 1 and <= 5)
                breakdown[review.Stars]++;
        }

        double averageRating;
        double fiveStarRatio;

        if (reviews.Count > 0)
        {
            averageRating = Math.Round(reviews.Average(r => r.Stars), 1);
            fiveStarRatio = breakdown[5] / (double)reviews.Count;
        }
        else
        {
            averageRating = Math.Round((double)driver.RatingAverage, 1);
            fiveStarRatio = driver.RatingCount > 0 ? 0.88 : 0;
        }

        var reviewDtos = reviews
            .Select(r => new CustomerReviewDto(
                r.Stars,
                r.Comment ?? string.Empty,
                FormatDateLabel(r.CreatedAt),
                r.TripId,
                r.CreatedAt))
            .ToList();

        return new DriverRatingsSummaryDto(
            averageRating,
            totalTrips > 0 ? totalTrips : driver.RatingCount,
            fiveStarRatio,
            breakdown,
            BuildTags(reviews.Select(r => r.Stars).ToList()),
            reviewDtos);
    }

    private static string FormatDateLabel(DateTime createdAt) =>
        createdAt.ToString("dd/MM/yyyy");

    private static IReadOnlyList<RatingTagStatDto> BuildTags(IReadOnlyList<int> starsList)
    {
        if (starsList.Count == 0)
            return [];

        var counts = new Dictionary<string, int>();
        for (var i = 0; i < starsList.Count; i++)
        {
            var tag = TagCatalog[i % TagCatalog.Length];
            counts[tag.Key] = counts.GetValueOrDefault(tag.Key) + 1;

            if (starsList[i] >= 4)
                counts["punctual"] = counts.GetValueOrDefault("punctual") + 1;
        }

        return counts
            .Select(entry =>
            {
                var label = TagCatalog.FirstOrDefault(t => t.Key == entry.Key).Label ?? entry.Key;
                return new RatingTagStatDto(entry.Key, label, entry.Value);
            })
            .OrderByDescending(t => t.Count)
            .ToList();
    }
}
