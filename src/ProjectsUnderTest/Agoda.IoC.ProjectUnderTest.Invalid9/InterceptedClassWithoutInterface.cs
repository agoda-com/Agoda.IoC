using Agoda.IoC.Core;
//using Agoda.Website.MVC.Core.StateManagement.Caching;

namespace Agoda.IoC.ProjectUnderTest.Invalid9
{
    public class BaseInterceptedClassWithoutInterface {}
    [RegisterSingleton]
    public class InterceptedClassWithoutInterface : BaseInterceptedClassWithoutInterface
    {
        //[Caching]
        //public int Whatever()
        //{
        //    return 1;
        //}
    }
}
