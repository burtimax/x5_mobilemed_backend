using System.Reflection;
using Application.QuartzJobs;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddMapster(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(
            Assembly.GetExecutingAssembly());
        config.RequireExplicitMapping = false;
        config.RequireDestinationMemberSource = false;

        config.When((srcType, destType, _) => true)
            .IgnoreNullValues(true);

        // config
        //     .When((srcType, destType, _) => srcType == typeof(IBaseBotEntityWithoutIdentity) == false && destType == typeof(IBaseBotEntityWithoutIdentity))
        //     .Ignore("Id",
        //         nameof(IBaseBotEntityWithoutIdentity.CreatedAt),
        //         nameof(IBaseBotEntityWithoutIdentity.UpdatedAt),
        //         nameof(IBaseBotEntityWithoutIdentity.DeletedAt));

        var mapperConfig = new Mapper(config);
        services.AddSingleton<IMapper>(mapperConfig);
    }
}
