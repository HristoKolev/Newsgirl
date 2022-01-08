namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using Testing;
    using Xunit;

    public class StructuredLoggerTest
    {
        [Fact]
        public async Task Log_is_noop_when_there_are_no_registered_configs()
        {
            var builder = new StructuredLoggerBuilder();

            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(Array.Empty<EventStreamConfig>());

                bool called = false;

                TestLogData LogFunc()
                {
                    called = true;
                    return new TestLogData();
                }

                logger.Log("NON_EXISTING_CONFIG", LogFunc);

                Assert.False(called);
            }
        }

        [Fact]
        public async Task Log_does_pass_data_to_consumers_when_config_is_available()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            var consumerMock = new LogConsumerMock(null);

            var expected = new List<TestLogData>();

            var builder = new StructuredLoggerBuilder();

            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogData>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(new[]
                {
                    new EventStreamConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Destinations = new[]
                        {
                            new EventDestinationConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true,
                            },
                        },
                    },
                });

                for (int i = 0; i < 500; i++)
                {
                    logger.Log(MOCK_KEY, () =>
                    {
                        var logData = new TestLogData();
                        expected.Add(logData);
                        return logData;
                    });
                }

                await Task.Delay(10);
            }

            AssertExt.SequentialEqual(expected, consumerMock.Logs);
        }

        [Fact]
        public async Task Log_not_sent_to_consumers_when_the_config_is_not_enabled()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            var consumerMock = new LogConsumerMock(null);

            var expected = new List<TestLogData>();

            var builder = new StructuredLoggerBuilder();

            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogData>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            await using (var logger = builder.Build())
            {
                for (int j = 0; j < 5; j++)
                {
                    await logger.Reconfigure(new[]
                    {
                        new EventStreamConfig
                        {
                            Name = MOCK_KEY,
                            Enabled = j % 2 == 1,
                            Destinations = new[]
                            {
                                new EventDestinationConfig
                                {
                                    Name = CONSUMER_NAME,
                                    Enabled = true,
                                },
                            },
                        },
                    });

                    for (int i = 0; i < 100; i++)
                    {
                        var logData = new TestLogData();

                        logger.Log(MOCK_KEY, () => logData);

                        if (j % 2 == 1)
                        {
                            expected.Add(logData);
                        }
                    }

                    await Task.Delay(10);
                }
            }

            AssertExt.SequentialEqual(expected, consumerMock.Logs);
        }

        [Fact]
        public async Task Log_not_sent_to_consumer_when_the_consumer_is_not_enabled()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            var consumerMock = new LogConsumerMock(null);

            var expected = new List<TestLogData>();

            var builder = new StructuredLoggerBuilder();
            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogData>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            await using (var logger = builder.Build())
            {
                for (int j = 0; j < 5; j++)
                {
                    await logger.Reconfigure(new[]
                    {
                        new EventStreamConfig
                        {
                            Name = MOCK_KEY,
                            Enabled = true,
                            Destinations = new[]
                            {
                                new EventDestinationConfig
                                {
                                    Name = CONSUMER_NAME,
                                    Enabled = j % 2 == 1,
                                },
                            },
                        },
                    });

                    for (int i = 0; i < 100; i++)
                    {
                        var logData = new TestLogData();

                        logger.Log(MOCK_KEY, () => logData);

                        if (j % 2 == 1)
                        {
                            expected.Add(logData);
                        }
                    }

                    await Task.Delay(10);
                }
            }

            AssertExt.SequentialEqual(expected, consumerMock.Logs);
        }

        [Fact]
        public async Task An_error_is_reported_when_a_consumer_throws()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            var errorReporter = new ErrorReporterMock();

            var consumerMock = new LogConsumerMock(errorReporter);

            var expected = new List<TestLogData>();

            var builder = new StructuredLoggerBuilder();
            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogData>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(new[]
                {
                    new EventStreamConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Destinations = new[]
                        {
                            new EventDestinationConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true,
                            },
                        },
                    },
                });

                for (int i = 0; i < 10; i++)
                {
                    var logData = new TestLogData();

                    if (i == 5)
                    {
                        await Task.Delay(10);

                        consumerMock.ShouldThrow = true;

                        logger.Log(MOCK_KEY, () => logData);

                        await Task.Delay(10);

                        consumerMock.ShouldThrow = false;
                    }
                    else
                    {
                        expected.Add(logData);
                        logger.Log(MOCK_KEY, () => logData);
                    }
                }
            }

            AssertExt.SequentialEqual(expected, consumerMock.Logs);

            Snapshot.MatchError(errorReporter.SingleException);
        }

        [Fact]
        public async Task Dispose_waits_for_the_consumers_to_finish()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            await using var errorReporter = new ErrorReporterMock();

            var consumerMock = new LogConsumerMock(errorReporter);

            var expected = new List<TestLogData>();

            var builder = new StructuredLoggerBuilder();
            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogData>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(new[]
                {
                    new EventStreamConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Destinations = new[]
                        {
                            new EventDestinationConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true,
                            },
                        },
                    },
                });

                consumerMock.WaitTime = TimeSpan.FromMilliseconds(10);

                for (int i = 0; i < 10; i++)
                {
                    var logData = new TestLogData();

                    expected.Add(logData);
                    logger.Log(MOCK_KEY, () => logData);
                }
            }

            AssertExt.SequentialEqual(expected, consumerMock.Logs);
        }

        [Fact]
        public async Task Log_calls_the_preprocessor_if_it_exists()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            var consumerMock = new LogConsumerMock(null);

            var expected = new List<TestLogData>();

            var builder = new StructuredLoggerBuilder();

            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogData>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            var preprocessor = new TestEventPreprocessor();

            await using (var logger = builder.Build())
            {
                var configs = new[]
                {
                    new EventStreamConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Destinations = new[]
                        {
                            new EventDestinationConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true,
                            },
                        },
                    },
                };

                await logger.Reconfigure(configs, preprocessor);

                for (int i = 0; i < 500; i++)
                {
                    logger.Log(MOCK_KEY, () =>
                    {
                        var logData = new TestLogData();
                        expected.Add(logData);
                        return logData;
                    });
                }

                await Task.Delay(10);
            }

            AssertExt.SequentialEqual(expected, consumerMock.Logs);

            foreach (var item in consumerMock.Logs)
            {
                Assert.Equal("test123", item.Str1);
            }
        }

        [Fact]
        public async Task Preprocessor_mutates_value_types()
        {
            const string MOCK_KEY = "MOCK_KEY";
            const string CONSUMER_NAME = "LogConsumerMock";

            var consumerMock = new LogConsumerStructMock(null);

            var expected = new List<TestLogDataStruct>();

            var builder = new StructuredLoggerBuilder();

            builder.AddEventStream(MOCK_KEY, new Dictionary<string, Func<EventDestination<TestLogDataStruct>>>
            {
                { CONSUMER_NAME, () => consumerMock },
            });

            var preprocessor = new TestEventPreprocessor();

            await using (var logger = builder.Build())
            {
                var configs = new[]
                {
                    new EventStreamConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Destinations = new[]
                        {
                            new EventDestinationConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true,
                            },
                        },
                    },
                };

                await logger.Reconfigure(configs, preprocessor);

                for (int i = 0; i < 500; i++)
                {
                    logger.Log(MOCK_KEY, () =>
                    {
                        var logData = new TestLogDataStruct();
                        expected.Add(logData);
                        return logData;
                    });
                }

                await Task.Delay(10);
            }

            Assert.Equal(expected.Count, consumerMock.Logs.Count);

            foreach (var item in consumerMock.Logs)
            {
                Assert.Equal("test123", item.Str1);
            }
        }
    }

    public class TestEventPreprocessor : EventPreprocessor
    {
        public void ProcessItem<TData>(ref TData item)
        {
            if (item is TestLogData x)
            {
                x.Str1 = "test123";
            }

            if (item is TestLogDataStruct y)
            {
                y.Str1 = "test123";
                item = (TData)(object)y;
            }
        }
    }

    public class TestLogData
    {
        public string Str1 { get; set; }
    }

    public struct TestLogDataStruct
    {
        public string Str1 { get; set; }
    }

    public class LogConsumerMock : EventDestination<TestLogData>
    {
        public List<TestLogData> Logs { get; } = new List<TestLogData>();

        public bool ShouldThrow { get; set; }

        public TimeSpan WaitTime { get; set; } = TimeSpan.Zero;

        public LogConsumerMock(ErrorReporter errorReporter) : base(errorReporter)
        {
            this.TimeBetweenRetries = TimeSpan.Zero;
            this.NumberOfRetries = 0;
            this.TimeBetweenMainLoopRestart = TimeSpan.Zero;
        }

        protected override async ValueTask Flush(ArraySegment<TestLogData> data)
        {
            if (this.WaitTime != TimeSpan.Zero)
            {
                await Task.Delay(this.WaitTime);
            }

            if (this.ShouldThrow)
            {
                throw new ApplicationException($"Throwing from inside of {nameof(LogConsumerMock)}.");
            }

            this.Logs.AddRange(data);
        }
    }

    public class LogConsumerStructMock : EventDestination<TestLogDataStruct>
    {
        public List<TestLogDataStruct> Logs { get; } = new List<TestLogDataStruct>();

        public bool ShouldThrow { get; set; }

        public TimeSpan WaitTime { get; set; } = TimeSpan.Zero;

        public LogConsumerStructMock(ErrorReporter errorReporter) : base(errorReporter)
        {
            this.TimeBetweenRetries = TimeSpan.Zero;
            this.NumberOfRetries = 0;
            this.TimeBetweenMainLoopRestart = TimeSpan.Zero;
        }

        protected override async ValueTask Flush(ArraySegment<TestLogDataStruct> data)
        {
            if (this.WaitTime != TimeSpan.Zero)
            {
                await Task.Delay(this.WaitTime);
            }

            if (this.ShouldThrow)
            {
                throw new ApplicationException($"Throwing from inside of {nameof(LogConsumerMock)}.");
            }

            this.Logs.AddRange(data);
        }
    }
}
