using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Agoda.IoC.Core
{
    [Serializable]
    public class RegistrationFailedException : Exception
    {
        private readonly List<RegistrationContextException> _registrationContextExceptions;

        public List<RegistrationContextException> RegistrationContextExceptions => _registrationContextExceptions;

        public RegistrationFailedException(string message)
            : base(message)
        {
        }
        public RegistrationFailedException(string message, List<RegistrationContextException> errors)
            : base(message)
        {
            _registrationContextExceptions = errors;
        }

        private RegistrationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
