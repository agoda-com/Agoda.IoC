using Agoda.IoC.Core;
using Agoda.IoC.ProjectUnderTest.Valid;

namespace Agoda.IoC.ProjectUnderTest.Valid2
{

    [RegisterTransient(ReplaceServices = true)]
    public class ReplaceServiceTwoWork : IReplaceService
    {

        public string DoWork { get; set; } = nameof(ReplaceServiceTwoWork);
    }

    public interface IServiceThatStartsUp
    {
        int Somedata { get; set; }
    }

    [RegisterSingleton(For = typeof(IServiceThatStartsUp))]
    public class ServiceThatStartsUp : IServiceThatStartsUp, IStartupable
    {
        public ServiceThatStartsUp()
        {
            Somedata = 0;
        }
        public int Somedata { get; set; }
        public void Start()
        {
            Somedata++;
        }
    }
}
