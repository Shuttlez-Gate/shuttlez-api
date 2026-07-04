using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
