using System;

namespace Core.Domain.Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when a concurrency conflict is detected during an operation.
    /// </summary>
    public class ConcurrencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConcurrencyException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConcurrencyException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 