namespace Newsgirl.Shared
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string Serialize<T>(T value) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var inputType = value.GetType();

            return JsonSerializer.Serialize(value, inputType, SerializationOptions);
        }

        public static Task Serialize<T>(Stream outputStream, T value) where T : class
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var inputType = value.GetType();

            return JsonSerializer.SerializeAsync(outputStream, value, inputType, SerializationOptions);
        }

        public static Task SerializeGenericType<T>(Stream outputStream, T value)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return JsonSerializer.SerializeAsync(outputStream, value, SerializationOptions);
        }

        public static T Deserialize<T>(string json) where T : class
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, SerializationOptions);
            }
            catch (Exception err)
            {
                long? bytePositionInLine = null;
                long? lineNumber = null;
                string jsonPath = null;

                if (err is JsonException jsonException)
                {
                    bytePositionInLine = jsonException.BytePositionInLine;
                    lineNumber = jsonException.LineNumber;
                    jsonPath = jsonException.Path;
                }

                throw new JsonHelperException("Failed deserialize json.")
                {
                    Details =
                    {
                        {"bytePositionInLine", bytePositionInLine},
                        {"lineNumber", lineNumber},
                        {"jsonPath", jsonPath},
                        {"inputJson", json},
                        {"outputType", typeof(T).FullName},
                    },
                };
            }
        }

        public static object Deserialize(ReadOnlySpan<byte> utf8Bytes, Type returnType)
        {
            if (utf8Bytes == default)
            {
                throw new ArgumentException("The utf8Bytes parameter must not be empty.", nameof(utf8Bytes));
            }

            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            try
            {
                return JsonSerializer.Deserialize(utf8Bytes, returnType, SerializationOptions);
            }
            catch (Exception err)
            {
                long? bytePositionInLine = null;
                long? lineNumber = null;
                string jsonPath = null;

                if (err is JsonException jsonException)
                {
                    bytePositionInLine = jsonException.BytePositionInLine;
                    lineNumber = jsonException.LineNumber;
                    jsonPath = jsonException.Path;
                }

                throw new JsonHelperException("Failed deserialize json.")
                {
                    Details =
                    {
                        {"bytePositionInLine", bytePositionInLine},
                        {"lineNumber", lineNumber},
                        {"jsonPath", jsonPath},
                        {"base64Data", Convert.ToBase64String(utf8Bytes)},
                        {"outputType", returnType.FullName},
                    },
                };
            }
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> utf8Bytes)
        {
            return (T) Deserialize(utf8Bytes, typeof(T));
        }
    }

    public class JsonHelperException : DetailedLogException
    {
        public JsonHelperException(string message) : base(message) { }
    }
}
