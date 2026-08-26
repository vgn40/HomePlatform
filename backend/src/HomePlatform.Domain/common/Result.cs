namespace HomePlatform.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Failure(string errorMessage)
    {
        return new Result(false, errorMessage);
    }
}
