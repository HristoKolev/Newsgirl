using System;

namespace Newsgirl.Shared.Infrastructure
{
    public class DateProvider : IDateProvider
    {
        public DateTime Now()
        {
            return DateTime.UtcNow;
        }
    }
    
    public interface IDateProvider
    {
        DateTime Now();
    }
}
