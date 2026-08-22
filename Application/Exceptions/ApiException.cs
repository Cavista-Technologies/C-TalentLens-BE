namespace C_TalentLens.Application.Exceptions;

public abstract class ApiException(
    string message,
    string title,
    int statusCode,
    string errorCode) : Exception(message)
{
    public string Title { get; } = title;

    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;
}

public class BadRequestException(string message, string errorCode = "bad_request")
    : ApiException(message, "The request is invalid.", StatusCodes.Status400BadRequest, errorCode);

public class NotFoundException(string message, string errorCode = "not_found")
    : ApiException(message, "The requested resource was not found.", StatusCodes.Status404NotFound, errorCode);

public class ConflictException(string message, string errorCode = "conflict")
    : ApiException(message, "The request conflicts with current state.", StatusCodes.Status409Conflict, errorCode);
