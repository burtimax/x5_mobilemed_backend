namespace Application.Utils;

public class WeigthUtil
{
    public static decimal Convert(int weigth, decimal? by100G)
    {
        return weigth * ((by100G ?? 0) /100);
    }
}
