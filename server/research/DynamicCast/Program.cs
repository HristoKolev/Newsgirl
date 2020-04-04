using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCast
{
    class Program
    {
        static void Main(string[] args)
        {
            var method = new DynamicMethod("convertTaskOfResult", typeof(object), new []{typeof(object)});

            var il = method.GetILGenerator();
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(Task<Result>).GetProperty("Result")?.GetMethod);
            il.Emit(OpCodes.Call, typeof(Result).GetProperty("ErrorMessages")?.GetMethod);
            il.Emit(OpCodes.Call, typeof(Result).GetMethods().First(x => x.Name == "Error" && x.IsGenericMethod && x.GetParameters().First().ParameterType == typeof(string[])).MakeGenericMethod(typeof(object)));
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")?.MakeGenericMethod(typeof(Result<object>)));
            il.Emit(OpCodes.Ret);

            var convert = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));

            var inArg = (object)Task.FromResult(Result.Error("123"));

            var outArg = convert(inArg);

            Console.WriteLine(outArg);
        }
    }
    
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