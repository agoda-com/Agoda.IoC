using System;
using System.Collections.Generic;

namespace Agoda.IoC.NetCore
{
    public class NetCoreKeyedRegistrations<T>
    {
        public NetCoreKeyedRegistrations(IDictionary<string, Type> registrations)
        {
            Registrations = registrations;
        }

        public IDictionary<string, Type> Registrations { get; }
    }
}