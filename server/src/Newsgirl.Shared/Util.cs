namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Toolkit.HighPerformance.Buffers;

    public static class EncodingHelper
    {
        /// <summary>
        /// An instance of UTF8Encoding that:
        /// * Does not add BOM when writing.
        /// * Throws when reading invalid input.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly UTF8Encoding UTF8 = new UTF8Encoding(false, true);
    }

    public static class EnvVariableHelper
    {
        /// <summary>
        /// Env variable by name.
        /// </summary>
        public static string Get(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DetailedException($"No ENV variable found for `{name}`.")
                {
                    Fingerprint = "ENV_VARIABLE_NOT_FOUND",
                };
            }

            return value;
        }
    }

    public static class DelegateHelper
    {
        public static Action Debounce(Action x, TimeSpan duration)
        {
            var semaphore = new SemaphoreSlim(1, 1);
            Timer timer = null;
            bool fired = false;

            void RestartTimer()
            {
                timer?.Dispose();
                timer = new Timer(s =>
                {
                    semaphore.Wait();

                    try
                    {
                        timer!.Dispose();
                        timer = null;

                        if (fired)
                        {
                            x();
                            fired = false;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, null, duration, duration);
            }

            return () =>
            {
                semaphore.Wait();

                try
                {
                    if (timer == null)
                    {
                        x();
                        RestartTimer();
                    }
                    else
                    {
                        fired = true;
                        RestartTimer();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            };
        }
    }

    public static class StreamExtensions
    {
        public static async ValueTask<IMemoryOwner<byte>> ReadUnknownSizeStream(this Stream source)
        {
            var buffer = new ArrayPoolBufferWriter<byte>();

            try
            {
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer.GetMemory(8192))) != 0)
                {
                    buffer.Advance(bytesRead);
                }

                return buffer;
            }
            catch (Exception)
            {
                buffer.Dispose();

                throw;
            }
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// I use this to prevent empty strings from being stored in the database.
        /// </summary>
        public static string SomethingOrNull(this string x)
        {
            return string.IsNullOrWhiteSpace(x) ? null : x;
        }
    }

    public static class RegexExtensions
    {
        public static bool IsOnlyMatch(this Regex regex, string input)
        {
            var matches = regex.Matches(input);

            return matches.Count == 1 && matches.First().Value == input;
        }
    }

    public static class DateTimeExtensions
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixEpochTime(this DateTime time)
        {
            return (long) (time - Epoch).TotalSeconds;
        }
    }

    public static class ReflectionEmmitHelper
    {
        static ReflectionEmmitHelper()
        {
            var assemblyName = new AssemblyName("DynamicAssembly+" + nameof(ReflectionEmmitHelper));
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        }

        public static ModuleBuilder ModuleBuilder { get; }

        public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate
        {
            return (T) methodInfo.CreateDelegate(typeof(T));
        }
    }
}
