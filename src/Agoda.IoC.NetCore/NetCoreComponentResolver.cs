using System;
using Agoda.IoC.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Agoda.IoC.NetCore
{
    public class NetCoreComponentResolver : IComponentResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public NetCoreComponentResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Resolve<T>()
        {
            return _serviceProvider.GetService<T>();

        }
    }
}