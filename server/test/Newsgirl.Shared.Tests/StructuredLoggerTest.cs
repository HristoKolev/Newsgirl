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
                await logger.Reconfigure(Array.Empty<StructuredLoggerConfig>());
                
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
            
            builder.AddConfig(MOCK_KEY, new Dictionary<string, Func<LogConsumer<TestLogData>>>
            {
                {CONSUMER_NAME, () => consumerMock}
            });
            
            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(new []
                {
                    new StructuredLoggerConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Consumers = new []
                        {
                            new StructuredLoggerConsumerConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true
                            }
                        }
                    } 
                });
                
                for (int j = 0; j < 5; j++)
                {
                    for (int i = 0; i < 100; i++)
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
            
            builder.AddConfig(MOCK_KEY, new Dictionary<string, Func<LogConsumer<TestLogData>>>
            {
                {CONSUMER_NAME, () => consumerMock}
            });
            
            await using (var logger = builder.Build())
            {
                for (int j = 0; j < 5; j++)
                {
                    await logger.Reconfigure(new []
                    {
                        new StructuredLoggerConfig
                        {
                            Name = MOCK_KEY,
                            Enabled = j % 2 == 1,
                            Consumers = new []
                            {
                                new StructuredLoggerConsumerConfig
                                {
                                    Name = CONSUMER_NAME,
                                    Enabled = true
                                }
                            }
                        } 
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
            builder.AddConfig(MOCK_KEY, new Dictionary<string, Func<LogConsumer<TestLogData>>>
            {
                {CONSUMER_NAME, () => consumerMock}
            });
            
            await using (var logger = builder.Build())
            {
                for (int j = 0; j < 5; j++)
                {
                    await logger.Reconfigure(new []
                    {
                        new StructuredLoggerConfig
                        {
                            Name = MOCK_KEY,
                            Enabled = true,
                            Consumers = new []
                            {
                                new StructuredLoggerConsumerConfig
                                {
                                    Name = CONSUMER_NAME,
                                    Enabled = j % 2 == 1
                                }
                            }
                        } 
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
            builder.AddConfig(MOCK_KEY, new Dictionary<string, Func<LogConsumer<TestLogData>>>
            {
                {CONSUMER_NAME, () => consumerMock}
            });
            
            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(new []
                {
                    new StructuredLoggerConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Consumers = new []
                        {
                            new StructuredLoggerConsumerConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true
                            }
                        }
                    } 
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
            builder.AddConfig(MOCK_KEY, new Dictionary<string, Func<LogConsumer<TestLogData>>>
            {
                {CONSUMER_NAME, () => consumerMock}
            });
            
            await using (var logger = builder.Build())
            {
                await logger.Reconfigure(new []
                {
                    new StructuredLoggerConfig
                    {
                        Name = MOCK_KEY,
                        Enabled = true,
                        Consumers = new []
                        {
                            new StructuredLoggerConsumerConfig
                            {
                                Name = CONSUMER_NAME,
                                Enabled = true
                            }
                        }
                    } 
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
    }

    public class TestLogData
    {
    }

    public class LogConsumerMock : LogConsumer<TestLogData>
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
}
