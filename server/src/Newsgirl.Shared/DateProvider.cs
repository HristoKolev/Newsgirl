namespace Newsgirl.Shared
{
    using System;

    public interface DateProvider
    {
        DateTime Now();
    }

    public class DateProviderImpl : DateProvider
    {
        public DateTime Now()
        {
            return DateTime.UtcNow;
        }
    }
}
