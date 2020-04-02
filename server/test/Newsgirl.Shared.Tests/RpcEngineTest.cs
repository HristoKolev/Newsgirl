using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newsgirl.Shared.Infrastructure;
using Xunit;

using Newsgirl.Testing;
using NSubstitute;

namespace Newsgirl.Shared.Tests
{
    public class RpcEngineTest
    {
        [Fact]
        public void Metadata_has_metadata_only_for_correctly_marked_types()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(RpcMarkingTest1),
                    typeof(RpcMarkingTest2),
                    typeof(RpcMarkingTest3),
                },
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            
            var metadata = rpcEngine.Metadata.Single();
            
            Assert.Equal(typeof(RpcMarkingTest3), metadata.HandlerClass);

            var methodInfos = typeof(RpcMarkingTest3).GetMethods().Where(x => x.DeclaringType == typeof(RpcMarkingTest3)).ToArray();
            
            Assert.Equal(methodInfos.Single(), metadata.HandlerMethod);
        }

        public class RpcMarkingTest1 {}

        public class RpcMarkingTest2
        {
            public Task NonRpcMethod() => Task.CompletedTask;
        }

        public class RpcMarkingTest3
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());
        }

        [Fact]
        public void Ctor_throws_when_static_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(StaticRpcMethodHandler),
                    },
                }, GetLog());
            });
        }
        
        public class StaticRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public static Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void Ctor_throws_when_private_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(PrivateRpcMethodHandler),
                    },
                }, GetLog());
            });
        }
            
        public class PrivateRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            private Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void Metadata_has_correct_request_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(RequestTypeTestHandler),
                },
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();
            
            Assert.Equal(typeof(SimpleRequest1), metadata.RequestType);
        }
        
        public class RequestTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());
        }

        [Fact]
        public void Metadata_has_correct_response_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResponseTypeTestHandler),
                },
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();
            
            Assert.Equal(typeof(SimpleResponse1), metadata.ResponseType);
        }
        
        public class ResponseTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());
        }

        [Fact]
        public void Ctor_throws_on_invalid_parameter_type()
        {
            Snapshot.MatchError(() =>
            {
                new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(InvalidParameterTestHandler),
                    },
                }, GetLog());
            });
        }
        
        public class InvalidParameterTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(StringBuilder sb) => Task.FromResult(new SimpleResponse1());
        }
        
        [Fact]
        public void Ctor_works_when_parameter_type_is_explicitly_allowed()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExplicitlyAllowParameterTypeTestHandler),
                },
                HandlerArgumentTypeWhiteList = new []
                {
                    typeof(StringBuilder)
                }
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();
            
            Assert.Equal(typeof(StringBuilder), metadata.Parameters.Single());
        }
        
        public class ExplicitlyAllowParameterTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(StringBuilder sb) => Task.FromResult(new SimpleResponse1());
        }
 
        [Fact]
        public void Ctor_throws_on_invalid_return_type()
        {
            Snapshot.MatchError(() =>
            {
                new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(InvalidReturnTypeTestHandler),
                    },
                }, GetLog());
            });
        }
        
        public class InvalidReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public StringBuilder RpcMethod(SimpleRequest1 req) => null;
        }


        [Fact]
        public void Ctor_throws_on_colliding_request_types()
        {
            Snapshot.MatchError(() =>
            {
                new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(CollidingRequestsTestHandler),
                    },
                }, GetLog());
            });
        }
        
        public class CollidingRequestsTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());

            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod2() => Task.FromResult(new SimpleResponse1());
        }

        [Fact]
        public void Metadata_has_class_level_supplemental_attributes()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesClassOnlyHandler),
                },
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();
            
            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(123, attribute.Value);
        }
        
        [TestSupplemental(123)]
        public class SupplementalAttributesClassOnlyHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());
        }
        
        [Fact]
        public void Metadata_has_method_level_supplemental_attributes()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesMethodOnlyHandler),
                },
            }, GetLog());

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(456, attribute.Value);
        }
        
        public class SupplementalAttributesMethodOnlyHandler
        {
            [TestSupplemental(456)]
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());
        }
        
        [Fact]
        public void Metadata_method_level_supplemental_attributes_takes_priority()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesTestHandler),
                },
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(456, attribute.Value);
        }
        
        [TestSupplemental(123)]
        public class SupplementalAttributesTestHandler
        {
            [TestSupplemental(456)]
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod() => Task.FromResult(new SimpleResponse1());
        }
        
        public class TestSupplementalAttribute : RpcSupplementalAttribute
        {
            public int Value { get; }

            public TestSupplementalAttribute(int value) => this.Value = value;
        }
        
        [Fact]
        public async Task Execute_throws_on_null_message_payload()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler)
                },
            }, GetLog());

            await Snapshot.MatchError(async () =>
            {
                await rpcEngine.Execute<ExecutorTestResponse>(null, GetDefaultInstanceProvider());
            });
        }
        
        [Fact]
        public async Task Execute_throws_when_it_cannot_match_a_handler()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler)
                },
            }, GetLog());

            await Snapshot.MatchError(async () =>
            {
                await rpcEngine.Execute<NonRegisteredResponse>(new NonRegisteredRequest(), GetDefaultInstanceProvider());
            });
        }
        
        [Fact]
        public async Task Execute_runs_the_handler_method()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler)
                },
            }, GetLog());

            ExecutorTestHandler.RunCount = 0;

            await rpcEngine.Execute<ExecutorTestResponse>(new ExecutorTestRequest(), GetDefaultInstanceProvider());
            
            Assert.Equal(1, ExecutorTestHandler.RunCount);
        }
        
        [Fact]
        public async Task Execute_passes_the_request_to_the_handler_method()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler)
                },
            }, GetLog());

            var request = new ExecutorTestRequest();
            
            await rpcEngine.Execute<ExecutorTestResponse>(request, GetDefaultInstanceProvider());
            
            Assert.Equal(request, ExecutorTestHandler.Request);
        }
        
        [Fact]
        public async Task Execute_returns_the_response_returned_from_the_handler_method()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler)
                },
            }, GetLog());

            var request = new ExecutorTestRequest
            {
                Number = 42
            };
            
            var result = await rpcEngine.Execute<ExecutorTestResponse>(request, GetDefaultInstanceProvider());

            Assert.Equal(result.Payload, ExecutorTestHandler.Response);
            
            Assert.Equal(43, result.Payload.Number);
        }
        
        [Fact]
        public async Task Execute_returns_error_result_when_the_handler_throws()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ThrowingExecutorTestHandler)
                },
            }, GetLog());
            
            var result = await rpcEngine.Execute<ExecutorTestResponse>(new ExecutorTestRequest(), GetDefaultInstanceProvider());
            
            Snapshot.Match(result);
        }
        
        public class ThrowingExecutorTestHandler
        {
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<ExecutorTestResponse> RpcMethod1(ExecutorTestRequest req)
            {
                throw new ApplicationException("Testing the exception handling.");
            }
        }
        
        public class ExecutorTestHandler
        {
            public static int RunCount = 0;

            public static ExecutorTestRequest Request;
            
            public static ExecutorTestResponse Response;
            
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<ExecutorTestResponse> RpcMethod1(ExecutorTestRequest req)
            {
                Request = req;
                RunCount += 1;

                var response = new ExecutorTestResponse
                {
                    Number = req.Number + 1,
                };

                Response = response;

                return Task.FromResult(response);
            }
        }

        [Fact]
        public void Ctor_works_correctly_with_null_middleware_param()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareTestHandler),
                },
                MiddlewareTypes = null,
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Empty(metadata.MiddlewareTypes);
        }
        
        [Fact]
        public void Ctor_works_correctly_with_empty_middleware_param()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareTestHandler),
                },
                MiddlewareTypes = Array.Empty<Type>(),
            }, GetLog());
            
            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Empty(metadata.MiddlewareTypes);
        }
        
        public class MiddlewareTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(SimpleRequest1 req) => Task.FromResult(new SimpleResponse1());
        }
        
        [Fact]
        public void Ctor_throws_when_middleware_does_not_implement_RpcMiddleware()
        {
            Snapshot.MatchError(() =>
            {
                new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(MiddlewareTestHandler),
                    },
                    MiddlewareTypes = new []
                    {
                        typeof(NonConformingMiddleware)
                    }
                }, GetLog());
            });
        }
        
        public class NonConformingMiddleware
        {
        }
        
        [Fact]
        public async Task Execute_calls_middleware_in_the_correct_order()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareOrderTestHandler),
                },
                MiddlewareTypes = new []
                {
                    typeof(MiddlewareOrderTestMiddleware1),
                    typeof(MiddlewareOrderTestMiddleware2),
                    typeof(MiddlewareOrderTestMiddleware3),
                }
            }, GetLog());

            var request = new MiddlewareTestRequest();
            
            await rpcEngine.Execute<MiddlewareTestResponse>(request, GetDefaultInstanceProvider());
            
            Snapshot.Match(request.Trace);
        }
        
        public class MiddlewareOrderTestHandler
        {
            [RpcBind(typeof(MiddlewareTestRequest), typeof(MiddlewareTestResponse))]
            public async Task<MiddlewareTestResponse> RpcMethod(MiddlewareTestRequest request)
            {
                request.Trace.Add(this.GetType().Name + "_HandlerMethod");
                
                return new MiddlewareTestResponse();
            }
        }
        
        public class MiddlewareTestRequest
        {
            public List<string> Trace { get; } = new List<string>();
        }
    
        public class MiddlewareTestResponse
        {
        }

        public class MiddlewareOrderTestMiddleware1 : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var request = (MiddlewareTestRequest) context.Request;
                
                request.Trace.Add(this.GetType().Name + "_Before");

                await next(context, instanceProvider);
                
                request.Trace.Add(this.GetType().Name + "_After");
            }
        }
        
        public class MiddlewareOrderTestMiddleware2 : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var request = (MiddlewareTestRequest) context.Request;
                
                request.Trace.Add(this.GetType().Name + "_Before");

                await next(context, instanceProvider);
                
                request.Trace.Add(this.GetType().Name + "_After");
            }
        }
        
        public class MiddlewareOrderTestMiddleware3 : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var request = (MiddlewareTestRequest) context.Request;
                
                request.Trace.Add(this.GetType().Name + "_Before");

                await next(context, instanceProvider);
                
                request.Trace.Add(this.GetType().Name + "_After");
            }
        }
        
        [Fact]
        public async Task Execute_additional_handler_arguments_are_supplied_from_the_items_dictionary()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(AdditionalArgumentsTestHandler),
                },
                MiddlewareTypes = new []
                {
                    typeof(AdditionalArgumentsMiddleware),
                },
                HandlerArgumentTypeWhiteList = new []
                {
                    typeof(AdditionalArgumentModel)
                }
            }, GetLog());

            var inArg = new AdditionalArgumentModel();
            AdditionalArgumentsMiddleware.AdditionalArg = inArg;
            
            var request = new SimpleRequest1();
            
            await rpcEngine.Execute<SimpleResponse1>(request, GetDefaultInstanceProvider());

            var outArg = AdditionalArgumentsTestHandler.AdditionalArg;
            
            Assert.Equal(inArg, outArg);
        }
        
        public class AdditionalArgumentsTestHandler
        {
            public static AdditionalArgumentModel AdditionalArg { get; set; }
            
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public async Task<SimpleResponse1> RpcMethod(SimpleRequest1 request, AdditionalArgumentModel additionalArgument)
            {
                AdditionalArg = additionalArgument;

                return new SimpleResponse1();
            }
        }
        
        public class AdditionalArgumentsMiddleware : RpcMiddleware
        {
            public static AdditionalArgumentModel AdditionalArg { get; set; }
            
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                context.Items.Add(typeof(AdditionalArgumentModel), AdditionalArg);
                
                await next(context, instanceProvider);
            }
        }
        
        public class AdditionalArgumentModel
        {
        }
        
        
        [Fact]
        public async Task Execute_correctly_returns_result_of_response_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResultOfResponseTypeTestHandler),
                },
            }, GetLog());

            var result = await rpcEngine.Execute<SimpleResponse1>(new SimpleRequest1(), GetDefaultInstanceProvider());
            
            Assert.Equal(ResultOfResponseTypeTestHandler.ResultValue, result);
        }
        
        public class ResultOfResponseTypeTestHandler
        {
            public static Result<SimpleResponse1> ResultValue { get; set; }
            
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public async Task<Result<SimpleResponse1>> RpcMethod(SimpleRequest1 request)
            {
                ResultValue = Result.Ok(new SimpleResponse1());
                return ResultValue;
            }
        }
        
        [Fact]
        public async Task Execute_correctly_returns_simple_result_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResultOfResponseTypeTestHandler),
                },
                MiddlewareTypes = new []
                {
                    typeof(SimpleResultMiddleware)
                }
            }, GetLog());

            var result = await rpcEngine.Execute<SimpleResponse1>(new SimpleRequest1(), GetDefaultInstanceProvider());
            
            Assert.Equal(SimpleResultMiddleware.ErrorMessage, result.ErrorMessages[0]);
        }
        
        public class SimpleResultMiddleware : RpcMiddleware
        {
            public static string ErrorMessage { get; } = "test123";
            
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                context.SetResponse(Result.Error(ErrorMessage));
            }
        }
        
        [Fact]
        public async Task Execute_returns_error_result_when_the_response_task_is_null()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResultOfResponseTypeTestHandler),
                },
                MiddlewareTypes = new []
                {
                    typeof(NullResponseTaskMiddleware)
                }
            }, GetLog());

            var result = await rpcEngine.Execute<SimpleResponse1>(new SimpleRequest1(), GetDefaultInstanceProvider());
            
            Snapshot.Match(result);
        }
        
        public class NullResponseTaskMiddleware : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                // do nothing, context.responseTask stays null 
            }
        }
        
        [Fact]
        public async Task Execute_returns_error_result_when_the_response_task_has_a_value_of_unsupported_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResultOfResponseTypeTestHandler),
                },
                MiddlewareTypes = new []
                {
                    typeof(UnsupportedResponseTypeMiddleware)
                }
            }, GetLog());

            var result = await rpcEngine.Execute<SimpleResponse1>(new SimpleRequest1(), GetDefaultInstanceProvider());
            
            Snapshot.Match(result);
        }
        
        public class UnsupportedResponseTypeMiddleware : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                context.SetResponse(new StringBuilder()); 
            }
        }
        
        private static InstanceProvider GetDefaultInstanceProvider()
        {
            var resolver = Substitute.For<InstanceProvider>();

            resolver.Get(null).ReturnsForAnyArgs(x => Activator.CreateInstance(x.Arg<Type>()));

            return resolver;
        }
        
        private static ILog GetLog()
        {
            return Substitute.For<ILog>();
        }
    }

    public class SimpleRequest1 {}
    
    public class SimpleResponse1 {}

    public class ExecutorTestRequest
    {
        public int Number { get; set; }
    }
    
    public class ExecutorTestResponse
    {
        public int Number { get; set; }
    }

    public class NonRegisteredRequest { }
    
    public class NonRegisteredResponse { }
}
