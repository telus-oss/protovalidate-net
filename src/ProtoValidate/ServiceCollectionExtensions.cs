using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProtoValidate;

public static class ServiceCollectionExtensions
{
    public static void AddProtoValidate(this IServiceCollection serviceCollection)
    {
        if (serviceCollection == null)
        {
            throw new ArgumentNullException(nameof(serviceCollection));
        }
        serviceCollection.TryAddSingleton<IValidator, Validator>();
    }

    public static void AddProtoValidate(this IServiceCollection serviceCollection, Action<ValidatorOptions> setOptions)
    {
        if (serviceCollection == null)
        {
            throw new ArgumentNullException(nameof(serviceCollection));
        }
        if (setOptions == null)
        {
            throw new ArgumentNullException(nameof(setOptions));
        }
       
        serviceCollection.TryAddSingleton<IValidator, Validator>();
        serviceCollection.Configure(setOptions);
    }
}