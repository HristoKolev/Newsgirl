namespace Newsgirl.WebServices.Infrastructure
{
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
        
        public static Result<T> FromErrorMessage<T>(string message)
        {
            return new Result<T>
            {
                ErrorMessages = new []
                {
                    message
                },
                IsSuccess = false
            };
        }
    }
    
    public class Result<T> : Result
    {
        public T Value { get; set; }
    }
}