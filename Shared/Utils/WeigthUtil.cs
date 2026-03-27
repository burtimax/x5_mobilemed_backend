namespace Application.Utils;

public class WeightUtil
{
    public static decimal Convert(int weight, decimal? by100G)
    {
        return weight * ((by100G ?? 0) /100);
    }
}
