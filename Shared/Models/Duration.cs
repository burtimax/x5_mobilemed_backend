namespace Shared.Models;

public class Duration
{
    private DateTimeOffset start;

    public Duration()
    {
        start = DateTimeOffset.Now;
    }

    public void Start()
    {
        start = DateTimeOffset.Now;
    }

    public double GetSeconds()
    {
        return (DateTimeOffset.Now - start).TotalSeconds;
    }
}
