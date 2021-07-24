using Agoda.IoC.Core;
using Agoda.IoC.ProjectUnderTest.Valid;

namespace Agoda.IoC.ProjectUnderTest.Valid2
{

    [RegisterTransient(ReplaceServices = true)]
    public class ReplaceServiceTwoWork : IReplaceService
    {

        public string DoWork { get; set; } = nameof(ReplaceServiceTwoWork);
    }
}
