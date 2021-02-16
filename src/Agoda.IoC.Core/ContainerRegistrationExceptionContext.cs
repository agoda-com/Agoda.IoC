using System;
using System.Collections.Generic;

namespace Agoda.IoC.Core
{
    public class ContainerRegistrationContextException : Exception
    {
        public ContainerRegistrationContextException(string message): base(message)
        {
        }
        public bool Ignore { get; set; }
        public List<RegistrationContextException> RegistrationContextExceptions { get; } 
             = new List<RegistrationContextException>();

    }


    public class RegistrationContextException : Exception
    {
        private readonly RegistrationContext _registrationContext;
        public RegistrationContextException(RegistrationContext registrationContext,string message) : base(message)
        {
            _registrationContext = registrationContext;
        }
    }
}
