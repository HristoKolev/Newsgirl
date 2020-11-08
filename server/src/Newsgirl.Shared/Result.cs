namespace Newsgirl.Shared
{
    /// <summary>
    /// Simple result type, uses generic T for the value and string[] for the errors.
    /// Defines a bunch of constructor methods for convenience.
    /// </summary>
    public class Result
    {
        public bool IsOk => this.ErrorMessages == default || this.ErrorMessages.Length == 0;

        public string[] ErrorMessages { get; set; }

        public static Result Ok()
        {
            return new Result();
        }

        public static Result<T> Ok<T>(T payload)
        {
            return new Result<T> {Payload = payload};
        }

        public static Result<T> Ok<T>()
        {
            return new Result<T> {Payload = default};
        }

        public static Result<T> Error<T>(string message)
        {
            return new Result<T> {ErrorMessages = new[] {message}};
        }

        public static Result<T> Error<T>(string[] errorMessages)
        {
            return new Result<T> {ErrorMessages = errorMessages};
        }

        public static Result Error(string message)
        {
            return new Result {ErrorMessages = new[] {message}};
        }

        public static Result Error(string[] errorMessages)
        {
            return new Result {ErrorMessages = errorMessages};
        }

        public virtual Result<object> ToGeneralForm()
        {
            return new Result<object>
            {
                ErrorMessages = this.ErrorMessages,
                Payload = null,
            };
        }
    }

    public class Result<T> : Result
    {
        public T Payload { get; set; }

        public override Result<object> ToGeneralForm()
        {
            return new Result<object>
            {
                Payload = this.Payload,
                ErrorMessages = this.ErrorMessages,
            };
        }

        public static implicit operator Result<T>(T x)
        {
            return Ok(x);
        }

        public static implicit operator Result<T>(string errorMessage)
        {
            return Error<T>(errorMessage);
        }

        public static implicit operator Result<T>(string[] errorMessages)
        {
            return Error<T>(errorMessages);
        }
    }
}
