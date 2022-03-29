using System;
using Agoda.IoC.Core;
using Autofac;

namespace Agoda.IoC.ProjectUnderTest.AutofacStartable
{
    public interface IStartupClass
    {
        bool IsStarted { get; set; }
    }
    [RegisterSingleton(For = typeof(IStartupClass))]
    public class StartupClass : IStartupClass, IStartable 
    {
        public bool IsStarted { get; set; }
        public StartupClass()
        {
            IsStarted = false;
        }
        public void Start()
        {
            IsStarted = true;
        }
    }

    public interface IStartupClass2
    {
        bool IsStarted { get; set; }
    }

    [RegisterSingleton(For = typeof(IStartupClass2))]
    public class StartupClass2 : IStartupClass2, IStartable
    {
        public bool IsStarted { get; set; }
        public StartupClass2()
        {
            IsStarted = false;
        }
        public void Start()
        {
            IsStarted = true;
        }
    }
}
