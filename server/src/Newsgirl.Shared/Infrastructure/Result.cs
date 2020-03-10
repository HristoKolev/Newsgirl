namespace Newsgirl.Shared.Infrastructure
{
    /// <summary>
    /// Simple result type, uses generic T for the value and string[] for the errors.
    /// Defines a bunch of constructor methods for convenience.  
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; set; }

        public string[] ErrorMessages { get; set; }
        
        public static Result<T> Success<T>(T payload)
        {
            return new Result<T>
            {
                Value = payload,
                IsSuccess = true
            };
        }
        
        public static Result<T> Success<T>()
        {
            return new Result<T>
            {
                Value = default,
                IsSuccess = true
            };
        }
        
        public static Result<T> FromErrorMessage<T>(string message)
        {
            return new Result<T>
            {
                ErrorMessages = new [] { message },
                IsSuccess = false
            };
        }

        public static Result FromErrorMessages<T>(string[] errorMessages)
        {
            return new Result<T>
            {
                ErrorMessages = errorMessages,
                IsSuccess = false
            };
        }
        
        public static Result Success()
        {
            return new Result
            {
                IsSuccess = true
            };
        }
        
        public static Result FromErrorMessage(string message)
        {
            return new Result
            {
                ErrorMessages = new [] { message },
                IsSuccess = false
            };
        }

        public static Result FromErrorMessages(string[] errorMessages)
        {
            return new Result
            {
                ErrorMessages = errorMessages,
                IsSuccess = false
            };
        }
    }
    
    public class Result<T> : Result
    {
        public T Value { get; set; }
    }
}
