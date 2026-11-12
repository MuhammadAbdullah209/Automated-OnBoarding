namespace AOB.BAL.Common.Models
{
    /// <summary>
    /// Uniform result returned by services so controllers never depend on exceptions
    /// for expected failures (invalid credentials, duplicate email, ...).
    /// </summary>
    public class ServiceResult
    {
        public bool Succeeded { get; protected set; }

        public string? Error { get; protected set; }

        public static ServiceResult Success() => new() { Succeeded = true };

        public static ServiceResult Failure(string error) => new() { Succeeded = false, Error = error };
    }

    /// <summary>
    /// ServiceResult that also carries a payload on success.
    /// </summary>
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; private set; }

        public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };

        public new static ServiceResult<T> Failure(string error) => new() { Succeeded = false, Error = error };
    }
}
