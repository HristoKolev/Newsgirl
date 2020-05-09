namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Testing;
    using Xunit;

    public class StructuredLoggerTest
    {
        [Fact]
        public void Ctor_throws_on_null_config_function()
        {
            Snapshot.MatchError(() =>
            {
                new StructuredLogger(null);
            });
        }
        
        [Fact]
        public async Task Log_is_noop_when_there_are_no_registered_configs()
        {
            void Configure(StructuredLoggerBuilder builder)
            {
            }
            
            await using (var logger = new StructuredLogger(Configure))
            {
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
            
            var consumerMock = new LogConsumerMock(null);
            
            void Configure(StructuredLoggerBuilder builder)
            {
                builder.AddConfig(MOCK_KEY, new LogConsumer<TestLogData>[] { consumerMock });
            }
            
            var sent = new List<TestLogData>();
            
            await using (var logger = new StructuredLogger(Configure))
            {
                for (int j = 0; j < 5; j++)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        logger.Log(MOCK_KEY, () =>
                        {
                            var logData = new TestLogData();
                            sent.Add(logData);
                            return logData;
                        });
                    }

                    await Task.Delay(10);
                }
            }
            
            AssertExt.SequentialEqual(sent, consumerMock.Logs);
        }
    }

    public class TestLogData
    {
    }

    public class LogConsumerMock : LogConsumerBase<TestLogData>
    {
        public List<TestLogData> Logs { get; } = new List<TestLogData>();
        
        public LogConsumerMock(ErrorReporter errorReporter) : base(errorReporter)
        {
        }

        protected override ValueTask ProcessBatch(ArraySegment<TestLogData> data)
        {
            this.Logs.AddRange(data);
            return new ValueTask();
        }
    }
}
