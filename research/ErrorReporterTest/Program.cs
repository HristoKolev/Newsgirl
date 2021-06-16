namespace ErrorReporterTest
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Newsgirl.Shared;

    internal static class Program
    {
        private static readonly Random random = new();

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray()
            );
        }

        private static async Task Main()
        {
            var errorReporter = new ErrorReporterImpl(new ErrorReporterImplConfig
            {
                Environment = "research",
                AppVersion = "1.0.0.0",
                InstanceName = "hristo-ws",
                SentryDsn = "http://06bc5208938e4f36abdd1ebe11a763c2@home-sentry.lan/5",
                SentryDebugMode = true,
            });

            string etc = RandomString((int) Math.Pow(1024, 2) - 2343);

            Console.WriteLine();

            await errorReporter.Error(new DetailedException("D") {Details = {{"etc", etc}}});
        }
    }
}
