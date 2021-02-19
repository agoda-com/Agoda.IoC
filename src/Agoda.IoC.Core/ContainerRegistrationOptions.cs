using System;

namespace Agoda.IoC.Core
{
    public class ContainerRegistrationOption
    {
        public Action<RegistrationContextException> OnRegistrationContextException { get; set; }
    }
}