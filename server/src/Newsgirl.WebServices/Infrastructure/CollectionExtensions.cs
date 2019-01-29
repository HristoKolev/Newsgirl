namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CollectionExtensions
    {
        public static IEnumerable<T1> IntersectBy<T1, T2>(
            this IEnumerable<T1> collection1, 
            IEnumerable<T2> collection2, 
            Func<T1, T2, bool> predicate)
        {
            return collection1.Where(t1 => collection2.FirstOrDefault(t2 => predicate(t1, t2)) != null);
        }
        
        public static IEnumerable<T1> ExceptBy<T1, T2>(
            this IEnumerable<T1> collection1, 
            IEnumerable<T2> collection2, 
            Func<T1, T2, bool> predicate)
        {
            return collection1.Where(t1 => collection2.FirstOrDefault(t2 => predicate(t1, t2)) == null);
        }
    }
}