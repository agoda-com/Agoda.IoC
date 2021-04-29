using System;
using Microsoft.Extensions.DependencyInjection;

namespace Agoda.IoC.NetCore
{
    public class KeyTypePair
    {
        public Type Type { get; set; }
        public string Key { get; set; }
        public ServiceLifetime ServiceLifetime { get; set; }

        public KeyTypePair(string key, Type type, ServiceLifetime serviceLifetime)
        {
            Key = key;
            Type = type;
            ServiceLifetime = serviceLifetime;
        }
    }
}