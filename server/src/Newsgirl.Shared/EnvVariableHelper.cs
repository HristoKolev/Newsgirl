namespace Newsgirl.Shared
{
    using System;

    public static class EnvVariableHelper
    {
        public static string Get(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DetailedLogException($"No ENV variable found for `{name}`.");
            }

            return value;
        }
    }
}
