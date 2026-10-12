namespace NEXUS.Models.ViewModels;

/**
 * One view serves every failure the app can render - 404, 403, 500 and the
 * rest. Keeping it in one place means the wording, the styling and the way
 * back home stay consistent instead of drifting page by page.
 */
public sealed class ErrorPage
{
    public int Code { get; init; } = 500;

    public string Title { get; init; } = "Something went wrong";

    public string Message { get; init; } = "An unexpected error occurred.";

    /** Correlation id, shown only for server faults so support can trace one. */
    public string? Reference { get; init; }

    public static ErrorPage For(int code) => code switch
    {
        400 => new ErrorPage
        {
            Code = 400,
            Title = "That request did not make sense",
            Message = "The form or address sent something the server could not read."
        },
        401 => new ErrorPage
        {
            Code = 401,
            Title = "Sign in required",
            Message = "You need to be signed in to view this page."
        },
        403 => new ErrorPage
        {
            Code = 403,
            Title = "Not your area",
            Message = "Your account does not have access to this page."
        },
        404 => new ErrorPage
        {
            Code = 404,
            Title = "Page not found",
            Message = "We could not find the page you were looking for."
        },
        405 => new ErrorPage
        {
            Code = 405,
            Title = "Method not allowed",
            Message = "That action is not available on this address."
        },
        429 => new ErrorPage
        {
            Code = 429,
            Title = "Too many requests",
            Message = "Slow down a moment and try again shortly."
        },
        _ => new ErrorPage
        {
            Code = code,
            Title = "Something went wrong",
            Message = "An unexpected error occurred while handling your request."
        }
    };
}
