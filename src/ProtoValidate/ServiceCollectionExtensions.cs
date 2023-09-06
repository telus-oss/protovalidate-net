using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProtoValidate;

public static class ServiceCollectionExtensions
{
    public static void AddProtoValidate(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection, nameof(serviceCollection));
        serviceCollection.TryAddSingleton<IValidator, Validator>();
    }

    public static void AddProtoValidate(this IServiceCollection serviceCollection, Action<ValidatorOptions> setOptions)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection, nameof(serviceCollection));
        ArgumentNullException.ThrowIfNull(setOptions, nameof(setOptions));

        serviceCollection.TryAddSingleton<IValidator, Validator>();
        serviceCollection.Configure(setOptions);
    }
}