using System;

namespace Agoda.IoC.Core
{
    public class RegistrationContextException : Exception
    {
        public RegistrationContext RegistrationContext { get; }
        public RegistrationContextException(RegistrationContext registrationContext,string message) : base(message)
        {
            RegistrationContext = registrationContext;
        }
    }
}
 