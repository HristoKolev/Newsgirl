namespace Newsgirl.Shared
{
    /// <summary>
    /// Simple result type, uses generic T for the value and string[] for the errors.
    /// Defines a bunch of constructor methods for convenience.  
    /// </summary>
    public class Result
    {
        public bool IsOk => this.ErrorMessages == null || this.ErrorMessages.Length == 0; 

        public string[] ErrorMessages { get; set; }
        
        public static Result Ok() => new Result();
        
        public static Result<T> Ok<T>(T payload) => new Result<T> { Payload = payload };

        public static Result<T> Ok<T>() => new Result<T> { Payload = default };

        public static Result<T> Error<T>(string message) => new Result<T> { ErrorMessages = new [] { message } };

        public static Result Error<T>(string[] errorMessages) => new Result<T> { ErrorMessages = errorMessages };

        public static Result Error(string message) => new Result { ErrorMessages = new [] { message } };

        public static Result Error(string[] errorMessages) => new Result { ErrorMessages = errorMessages };
    }
    
    public class Result<T> : Result
    {
        public T Payload { get; set; }
    }
}
