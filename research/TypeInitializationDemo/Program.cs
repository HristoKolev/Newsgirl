namespace TypeInitializationDemo
{
    using System;
    using System.Threading;

    public class Cats
    {
        public static string x = Log("x initialized");
        public static string y = Log("y initialized", true);
        
        static  Cats()
        {
            Console.WriteLine("Cats type init");
        }
        
        public static string z = Log("z initialized");
        
        public static string Log(string message, bool shouldThrow = false)
        {
            if (shouldThrow)
            {
                Thread.Sleep(100);
                
                throw new ApplicationException($"thr + {message} + {new Random().Next()}");
            }
            
            Console.WriteLine(message);
            return message;
        }
    }

    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("start of Main");


            Exception err1 = null;
            
            try
            {
                Cats.Log("static func");
            }
            catch (Exception e)
            {
                err1 = e;
            }
            
            Console.WriteLine("after static func invocation.");

            Exception err2 = null;
            
            try
            {
                SomeFunc();
            }
            catch (Exception e)
            {
                err2 = e;
            }

            Console.WriteLine(err1 == err2);

            new Cats();
        }

        static void SomeFunc()
        {
            Console.WriteLine("start of SomeFunc");
            
            Console.WriteLine(Cats.x.Length);
        }
    }
}
