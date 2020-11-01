using System;
using System.Runtime.Serialization;

namespace Agoda.IoC.Core
{
    [Serializable]
    public class RegistrationFailedException : Exception
    {
        public RegistrationFailedException(string message)
            : base(message)
        {
        }
        
        private RegistrationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
