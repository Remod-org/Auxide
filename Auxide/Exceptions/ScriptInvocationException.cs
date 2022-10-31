using System;

namespace Auxide.Exceptions
{
    public class ScriptInvocationException : Exception
    {
        public ScriptInvocationException(string message, Exception innerException = null) : base(message, innerException)
        {
            return;
        }
    }
}
