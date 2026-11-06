using System.Net;

namespace FSH.Framework.Core.Exceptions;

public class ConflictException : CustomException
{
    public ConflictException(string message)
        : base(
            message,
            Array.Empty<string>(),
            HttpStatusCode.Conflict)
    {    
    }
    public ConflictException()
    : this("Resource conflict.")
    {
    }
    public ConflictException(string message, Exception innerException)
    : base(
        message,
        innerException,
        HttpStatusCode.Conflict)
    {
    }
}