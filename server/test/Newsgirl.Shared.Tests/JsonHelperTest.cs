namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Testing;
    using Xunit;

    public class JsonHelperTest
    {
        [Fact]
        public async Task SerializeToStream_gives_correct_result()
        {
            var obj = TestJsonPayload.Create();

            await using (var memStream = new MemoryStream())
            {
                await JsonHelper.Serialize(memStream, obj);
                string json = EncodingHelper.UTF8.GetString(memStream.GetBuffer(), 0, (int) memStream.Length);
                Snapshot.MatchJson(json);
            }
        }

        [Fact]
        public async Task SerializeToStream_throws_on_null_stream()
        {
            var obj = TestJsonPayload.Create();

            await Snapshot.MatchError(async () =>
            {
                await JsonHelper.Serialize(null, obj);
            });
        }

        [Fact]
        public async Task SerializeToStream_throws_on_null_value()
        {
            await Snapshot.MatchError(async () =>
            {
                await JsonHelper.Serialize<object>(new MemoryStream(), null);
            });
        }

        [Fact]
        public async Task SerializeToStreamGeneric_gives_correct_result()
        {
            var value = TestJsonPayloadStruct.Create();

            await using (var memStream = new MemoryStream())
            {
                await JsonHelper.SerializeGenericType(memStream, value);
                string json = EncodingHelper.UTF8.GetString(memStream.GetBuffer(), 0, (int) memStream.Length);
                Snapshot.MatchJson(json);
            }
        }

        [Fact]
        public async Task SerializeToStreamGeneric_throws_on_null_stream()
        {
            var obj = TestJsonPayload.Create();

            await Snapshot.MatchError(async () =>
            {
                await JsonHelper.SerializeGenericType(null, obj);
            });
        }

        [Fact]
        public async Task SerializeToStreamGeneric_throws_on_null_value()
        {
            await Snapshot.MatchError(async () =>
            {
                await JsonHelper.SerializeGenericType<object>(new MemoryStream(), null);
            });
        }

        [Fact]
        public void Serialize_gives_correct_result()
        {
            var obj = TestJsonPayload.Create();
            string json = JsonHelper.Serialize(obj);
            Snapshot.MatchJson(json);
        }

        [Fact]
        public void Serialize_throws_on_null_value()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Serialize<object>(null);
            });
        }

        public class TestJsonPayload
        {
            private static TestJsonPayload CreateSimple()
            {
                return new TestJsonPayload
                {
                    Str1 = "test1",
                    Int1 = 123,
                    Dec1 = 123.456m,
                    Date1 = new DateTime(3000, 1, 1, 1, 1, 1, DateTimeKind.Utc),
                };
            }

            public static object Create()
            {
                var obj = CreateSimple();
                obj.InnerObject = CreateSimple();
                var arrayItem = CreateSimple();
                var listItem = CreateSimple();
                listItem.InnerDict = new Dictionary<string, TestJsonPayload>
                {
                    {"key1", CreateSimple()},
                    {"key2", CreateSimple()},
                    {"key3", CreateSimple()},
                };
                arrayItem.InnerList = new List<TestJsonPayload>
                {
                    CreateSimple(),
                    CreateSimple(),
                    listItem,
                };
                obj.InnerObject.InnerArray = new[]
                {
                    CreateSimple(),
                    arrayItem,
                };

                return obj;
            }

            public int Int1 { get; set; }

            public decimal Dec1 { get; set; }

            public string Str1 { get; set; }

            public DateTime Date1 { get; set; }

            public TestJsonPayload InnerObject { get; set; }

            public TestJsonPayload[] InnerArray { get; set; }

            public List<TestJsonPayload> InnerList { get; set; }

            public Dictionary<string, TestJsonPayload> InnerDict { get; set; }
        }

        public struct TestJsonPayloadStruct
        {
            private static TestJsonPayloadStruct CreateSimple()
            {
                return new TestJsonPayloadStruct
                {
                    Str1 = "test1",
                    Int1 = 123,
                    Dec1 = 123.456m,
                    Date1 = new DateTime(3000, 1, 1, 1, 1, 1, DateTimeKind.Utc),
                };
            }

            public static TestJsonPayloadStruct Create()
            {
                var obj = CreateSimple();
                obj.InnerDict = new Dictionary<string, TestJsonPayloadStruct>
                {
                    {"key1", CreateSimple()},
                    {"key2", CreateSimple()},
                    {"key3", CreateSimple()},
                };
                obj.InnerList = new List<TestJsonPayloadStruct>
                {
                    CreateSimple(),
                    CreateSimple(),
                    CreateSimple(),
                };

                obj.InnerArray = new[]
                {
                    CreateSimple(),
                    CreateSimple(),
                    CreateSimple(),
                };

                return obj;
            }

            public int Int1 { get; set; }

            public decimal Dec1 { get; set; }

            public string Str1 { get; set; }

            public DateTime Date1 { get; set; }

            public TestJsonPayloadStruct[] InnerArray { get; set; }

            public List<TestJsonPayloadStruct> InnerList { get; set; }

            public Dictionary<string, TestJsonPayloadStruct> InnerDict { get; set; }
        }
    }
}
