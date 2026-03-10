namespace Shared.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateOnly AsDateOnly(this DateTimeOffset dateTime)
    {
        return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
    }
    
    public static TimeOnly AsTimeOnly(this DateTimeOffset dateTime)
    {
        return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
    }
    
    public static DateTimeOffset ToOffset(this DateTimeOffset dateTime, int offset)
    {
        return dateTime.ToOffset(TimeSpan.FromHours(offset));
    }
}