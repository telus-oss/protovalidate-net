// Copyright 2023 TELUS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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