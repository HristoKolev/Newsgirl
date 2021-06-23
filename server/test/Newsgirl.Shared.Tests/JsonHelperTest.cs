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

        [Fact]
        public void Deserialize_throws_on_null_json()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize((string) null, typeof(TestJsonPayload));
            });
        }

        [Fact]
        public void Deserialize_throws_on_null_return_type()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize(JsonTestFiles.TEST1, null);
            });
        }

        [Fact]
        public void Deserialize_returns_correct_result()
        {
            var obj = (TestJsonPayload) JsonHelper.Deserialize(JsonTestFiles.TEST1, typeof(TestJsonPayload));
            TestJsonPayload.AssertCorrectObject(obj);
        }

        [Fact]
        public void Deserialize_throws_on_invalid_json()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize(JsonTestFiles.INVALID_JSON, typeof(TestJsonPayload));
            });
        }

        [Fact]
        public void DeserializeOfT_throws_on_null_json()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize<TestJsonPayload>((string) null);
            });
        }

        [Fact]
        public void DeserializeOfT_returns_correct_result()
        {
            var obj = JsonHelper.Deserialize<TestJsonPayload>(JsonTestFiles.TEST1);
            TestJsonPayload.AssertCorrectObject(obj);
        }

        [Fact]
        public void DeserializeOfT_throws_on_invalid_json()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize<TestJsonPayload>(JsonTestFiles.INVALID_JSON);
            });
        }

        [Fact]
        public void DeserializeSpan_throws_on_empty_span()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize((ReadOnlySpan<byte>) default, typeof(TestJsonPayload));
            });
        }

        [Fact]
        public void DeserializeSpan_throws_on_null_type()
        {
            var bytes = new byte[10];

            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize(bytes, null);
            });
        }

        [Fact]
        public void DeserializeSpan_returns_correct_result()
        {
            var bytes = EncodingHelper.UTF8.GetBytes(JsonTestFiles.TEST1);
            var result = (TestJsonPayload) JsonHelper.Deserialize(bytes, typeof(TestJsonPayload));
            TestJsonPayload.AssertCorrectObject(result);
        }

        [Fact]
        public void DeserializeSpan_throws_on_invalid_json()
        {
            var bytes = EncodingHelper.UTF8.GetBytes(JsonTestFiles.INVALID_JSON);

            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize(bytes, typeof(TestJsonPayload));
            });
        }

        [Fact]
        public void DeserializeSpanOfT_throws_on_empty_span()
        {
            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize<TestJsonPayload>((ReadOnlySpan<byte>) default);
            });
        }

        [Fact]
        public void DeserializeSpanOfT_returns_correct_result()
        {
            var bytes = EncodingHelper.UTF8.GetBytes(JsonTestFiles.TEST1);
            var result = JsonHelper.Deserialize<TestJsonPayload>(bytes);
            TestJsonPayload.AssertCorrectObject(result);
        }

        [Fact]
        public void DeserializeSpanOfT_throws_on_invalid_json()
        {
            var bytes = EncodingHelper.UTF8.GetBytes(JsonTestFiles.INVALID_JSON);

            Snapshot.MatchError(() =>
            {
                JsonHelper.Deserialize<TestJsonPayload>(bytes);
            });
        }

        [Fact]
        public void GetJsonSize_returns_correct_result()
        {
            var obj = TestJsonPayload.Create();

            int expectedSize = EncodingHelper.UTF8.GetBytes(JsonHelper.Serialize(obj)).Length;
            int actualSize = JsonHelper.GetJsonSize(obj);

            Assert.Equal(expectedSize, actualSize);
        }

        [Fact]
        public void GetJsonSize_returns_correct_result_on_null_value()
        {
            int result = JsonHelper.GetJsonSize<object>(null);

            Assert.Equal(4, result);
        }

        public class TestJsonPayload
        {
            private static readonly DateTime TestDate1 = new DateTime(3000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            private const string TEST_STR1 = "test1";
            private const int TEST_INT1 = 123;
            private const decimal TEST_DEC1 = 123.456m;

            private static TestJsonPayload CreateSimple()
            {
                return new TestJsonPayload
                {
                    Str1 = TEST_STR1,
                    Int1 = TEST_INT1,
                    Dec1 = TEST_DEC1,
                    Date1 = TestDate1,
                };
            }

            public static TestJsonPayload Create()
            {
                var obj = CreateSimple();
                obj.InnerObject = CreateSimple();
                obj.InnerDict = new Dictionary<string, TestJsonPayload>
                {
                    {"key1", CreateSimple()},
                    {"key2", CreateSimple()},
                    {"key3", CreateSimple()},
                };
                obj.InnerList = new List<TestJsonPayload>
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

            public static void AssertCorrectObject(TestJsonPayload obj)
            {
                AssertObjectData(obj);
                AssertObjectData(obj.InnerObject);

                foreach (var item in obj.InnerArray)
                {
                    AssertObjectData(item);
                }

                foreach (var item in obj.InnerList)
                {
                    AssertObjectData(item);
                }

                foreach (var item in obj.InnerDict.Values)
                {
                    AssertObjectData(item);
                }
            }

            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            private static void AssertObjectData(TestJsonPayload obj)
            {
                Assert.Equal(TEST_STR1, obj.Str1);
                Assert.Equal(TEST_INT1, obj.Int1);
                Assert.Equal(TEST_DEC1, obj.Dec1);
                Assert.Equal(TestDate1, obj.Date1);
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
            private static readonly DateTime TestDate1 = new DateTime(3000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            private const string TEST_STR1 = "test1";
            private const int TEST_INT1 = 123;
            private const decimal TEST_DEC1 = 123.456m;

            private static TestJsonPayloadStruct CreateSimple()
            {
                return new TestJsonPayloadStruct
                {
                    Str1 = TEST_STR1,
                    Int1 = TEST_INT1,
                    Dec1 = TEST_DEC1,
                    Date1 = TestDate1,
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

            public static void AssertCorrectObject(TestJsonPayloadStruct obj)
            {
                AssertObjectData(obj);

                foreach (var item in obj.InnerArray)
                {
                    AssertObjectData(item);
                }

                foreach (var item in obj.InnerList)
                {
                    AssertObjectData(item);
                }

                foreach (var item in obj.InnerDict.Values)
                {
                    AssertObjectData(item);
                }
            }

            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            private static void AssertObjectData(TestJsonPayloadStruct obj)
            {
                Assert.Equal(TEST_STR1, obj.Str1);
                Assert.Equal(TEST_INT1, obj.Int1);
                Assert.Equal(TEST_DEC1, obj.Dec1);
                Assert.Equal(TestDate1, obj.Date1);
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

    public static class JsonTestFiles
    {
        public const string INVALID_JSON = "{ \"data\": 12345CAT }";

        public const string TEST1 = @"
        {
          ""int1"": 123,
          ""dec1"": 123.456,
          ""str1"": ""test1"",
          ""date1"": ""3000-01-01T01:01:01Z"",
          ""innerObject"": {
            ""int1"": 123,
            ""dec1"": 123.456,
            ""str1"": ""test1"",
            ""date1"": ""3000-01-01T01:01:01Z"",
            ""innerObject"": null,
            ""innerArray"": null,
            ""innerList"": null,
            ""innerDict"": null
          },
          ""innerArray"": [
            {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            },
            {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            },
            {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            }
          ],
          ""innerList"": [
            {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            },
            {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            },
            {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            }
          ],
          ""innerDict"": {
            ""key1"": {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            },
            ""key2"": {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            },
            ""key3"": {
              ""int1"": 123,
              ""dec1"": 123.456,
              ""str1"": ""test1"",
              ""date1"": ""3000-01-01T01:01:01Z"",
              ""innerObject"": null,
              ""innerArray"": null,
              ""innerList"": null,
              ""innerDict"": null
            }
          }
        }";
    }
}
