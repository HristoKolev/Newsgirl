namespace TypeInitializationDemo
{
    using System;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            var my1 = new My1
            {
                X = 1,
                Y = 1,
            };

            My2 my2 = (My2)my1;

            Console.WriteLine(my2.X);
            Console.WriteLine(my2.Y);
        }
    }

    public class My1
    {
        public int X { get; set; }

        public int Y { get; set; }

        public static implicit operator My2(My1 my1)
        {
            return new My2
            {
                X = my1.X,
                Y = my1.Y,
            };
        }
    }
    
    public class My2
    {
        public int X { get; set; }

        public int Y { get; set; }
        //
        // public static explicit operator My2(My1 my1)
        // {
        //     return new My2
        //     {
        //         X = my1.X +1,
        //         Y = my1.Y +1,
        //     };
        // }
    }

    public class Operators
    {
        
    }
}
