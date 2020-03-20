namespace Newsgirl.Shared
{
    /// <summary>
    /// Simple result type, uses generic T for the value and string[] for the errors.
    /// Defines a bunch of constructor methods for convenience.  
    /// </summary>
    public class Result
    {
        public bool IsOk { get; set; }

        public string[] ErrorMessages { get; set; }
        
        public static Result<T> Ok<T>(T payload)
        {
            return new Result<T>
            {
                Value = payload,
                IsOk = true
            };
        }
        
        public static Result<T> Ok<T>()
        {
            return new Result<T>
            {
                Value = default,
                IsOk = true
            };
        }
        
        public static Result<T> Error<T>(string message)
        {
            return new Result<T>
            {
                ErrorMessages = new [] { message },
                IsOk = false
            };
        }

        public static Result Error<T>(string[] errorMessages)
        {
            return new Result<T>
            {
                ErrorMessages = errorMessages,
                IsOk = false
            };
        }
        
        public static Result Ok()
        {
            return new Result
            {
                IsOk = true
            };
        }
        
        public static Result Error(string message)
        {
            return new Result
            {
                ErrorMessages = new [] { message },
                IsOk = false
            };
        }

        public static Result Error(string[] errorMessages)
        {
            return new Result
            {
                ErrorMessages = errorMessages,
                IsOk = false
            };
        }
    }
    
    public class Result<T> : Result
    {
        public T Value { get; set; }
    }
}
