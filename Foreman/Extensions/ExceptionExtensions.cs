namespace Foreman.Extensions
{
    using System;
    using System.Reflection;
    using System.Security;
    using System.Threading;

    public static class ExceptionExtensions
    {
        public static bool IsCriticalApplicationException(this Exception exception)
        {
            exception = Unwrap(exception);
            return
                exception is StackOverflowException ||
                exception is OutOfMemoryException ||
                exception is ThreadAbortException ||
                exception is SecurityException;
        }

        public static Exception Unwrap(this Exception exception)
        {
            while (exception.InnerException != null && exception is TargetInvocationException)
                exception = exception.InnerException;

            return exception;
        }
    }
}
