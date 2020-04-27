namespace AsyncLocalDemo
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        public static AsyncLocal<int> Local = new AsyncLocal<int>();
        
        static async Task Main(string[] args)
        {
            await Task.Delay(1);
            Console.WriteLine($"main before: {Local.Value}");
            await Task.Delay(1);
            
            await Work1();
            
            await Task.Delay(1);
            Console.WriteLine($"main after: {Local.Value}");
            await Task.Delay(1);
        }

        private static async Task Work1()
        {
            Local.Value = 1;
            
            await Task.Delay(1);
            Console.WriteLine($"Work 1 before: {Local.Value}");
            await Task.Delay(1);
            
            await Work2();
            
            await Task.Delay(1);
            Console.WriteLine($"Work 1 after: {Local.Value}");
            await Task.Delay(1);
        }

        private static async Task Work2()
        {
            await Task.Delay(1);
            Console.WriteLine($"Work 2 before: {Local.Value}");
            await Task.Delay(1);
            
            await Work3();
            
            await Task.Delay(1);
            Console.WriteLine($"Work 2 after: {Local.Value}");
            await Task.Delay(1);
        }

        private static async Task Work3()
        {
            await Task.Delay(1);
            Console.WriteLine($"Work 3: {Local.Value}");
            await Task.Delay(1);
        }
    }
}
