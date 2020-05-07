using System;

namespace ErrorHandlingDemo
{
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine("TaskSchedulerOnUnobservedTaskException");
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("CurrentDomainOnUnhandledException");
        }
    }
}
