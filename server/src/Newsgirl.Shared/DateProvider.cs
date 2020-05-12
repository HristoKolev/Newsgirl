namespace Newsgirl.Shared
{
    using System;

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
