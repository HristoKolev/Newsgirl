using System;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher.Tests.Infrastructure
{
    public class DateProviderStub : IDateProvider
    {
        private readonly DateTime value;

        public DateProviderStub(DateTime value)
        {
            this.value = value;
        }
        
        public DateTime Now()
        {
            return this.value;
        }
    }
}