namespace VinhKhanhGuide.App.Models;

public class AuthResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public AuthSession? Session { get; init; }

    public static AuthResult Success(AuthSession session, string message)
    {
        return new AuthResult
        {
            Succeeded = true,
            Message = message,
            Session = session
        };
    }

    public static AuthResult Success(string message)
    {
        return new AuthResult
        {
            Succeeded = true,
            Message = message
        };
    }

    public static AuthResult Failure(string message)
    {
        return new AuthResult
        {
            Succeeded = false,
            Message = message
        };
    }
}
