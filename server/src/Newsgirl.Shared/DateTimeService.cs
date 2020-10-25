namespace Newsgirl.Shared
{
    using System;

    public interface DateTimeService
    {
        DateTime EventTime();

        DateTime CurrentTime();
    }

    public class DateTimeServiceImpl : DateTimeService
    {
        private readonly DateTime eventTime;

        public DateTimeServiceImpl()
        {
            this.eventTime = DateTime.UtcNow;
        }

        public DateTime EventTime()
        {
            return this.eventTime;
        }

        public DateTime CurrentTime()
        {
            return DateTime.UtcNow;
        }
    }
}
