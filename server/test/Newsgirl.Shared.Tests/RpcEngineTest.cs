// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global

namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Logging;
    using NSubstitute;
    using Testing;
    using Xunit;

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
            });

            Assert.Single(rpcEngine.Metadata);

            var metadata = rpcEngine.Metadata.Single();

            Assert.Equal(typeof(RpcMarkingTest3), metadata.DeclaringType);

            var methodInfos = typeof(RpcMarkingTest3).GetMethods().Where(x => x.DeclaringType == typeof(RpcMarkingTest3)).ToArray();

            Assert.Equal(methodInfos.Single(), metadata.HandlerMethod);
        }

        public class RpcMarkingTest1 { }

        public class RpcMarkingTest2
        {
            public Task NonRpcMethod()
            {
                return Task.CompletedTask;
            }
        }

        public class RpcMarkingTest3
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_when_static_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(StaticRpcMethodHandler),
                    },
                });
            });
        }

        public class StaticRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public static Task RpcMethod()
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Ctor_throws_when_non_public_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(NonPublicRpcMethodHandler),
                    },
                });
            });
        }

        public class NonPublicRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            private Task RpcMethod()
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Ctor_throws_when_virtual_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(VirtualRpcMethodHandler),
                    },
                });
            });
        }

        public class VirtualRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public virtual Task RpcMethod()
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Ctor_throws_when_handler_declaring_type_is_abstract()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(AbstractRpcMethodHandler),
                    },
                });
            });
        }

        public abstract class AbstractRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_when_handler_declaring_type_is_an_interface()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(InterfaceTypeRpcMethodHandler),
                    },
                });
            });
        }

        public interface InterfaceTypeRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_when_handler_declaring_type_is_a_struct()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(StructTypeRpcMethodHandler),
                    },
                });
            });
        }

        public struct StructTypeRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_when_request_type_is_not_a_reference_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(StructRequestHandler),
                    },
                });
            });
        }

        public class StructRequestHandler
        {
            [RpcBind(typeof(StructRequestTypeRequest), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        public struct StructRequestTypeRequest { }

        [Fact]
        public void Ctor_throws_when_response_type_is_not_a_reference_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(StructResponseHandler),
                    },
                });
            });
        }

        public class StructResponseHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(StructResponseTypeResponse))]
            public Task<StructResponseTypeResponse> RpcMethod()
            {
                return Task.FromResult(new StructResponseTypeResponse());
            }
        }

        public struct StructResponseTypeResponse { }

        [Fact]
        public void Ctor_throws_on_null_request_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(NullRequestTypeHandler),
                    },
                });
            });
        }

        public class NullRequestTypeHandler
        {
            [RpcBind(null, typeof(SimpleResponse1))]
            public Task RpcMethod()
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Ctor_throws_on_null_response_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(NullResponseTypeHandler),
                    },
                });
            });
        }

        public class NullResponseTypeHandler
        {
            [RpcBind(typeof(SimpleRequest1), null)]
            public Task RpcMethod()
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Metadata_entry_has_correct_request_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(RequestTypeTestHandler),
                },
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Equal(typeof(SimpleRequest1), metadata.RequestType);
        }

        public class RequestTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Metadata_entry_has_correct_response_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResponseTypeTestHandler),
                },
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Equal(typeof(SimpleResponse1), metadata.ResponseType);
        }

        public class ResponseTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_on_unrecognized_method_parameter_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(UnrecognizedParameterTestHandler),
                    },
                });
            });
        }

        public class UnrecognizedParameterTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(StringBuilder sb)
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_allows_whitelisted_parameter_types()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(AllowWhitelistedParameterTypeTestHandler),
                },
                ParameterTypeWhitelist = new[]
                {
                    typeof(StringBuilder),
                },
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Equal(typeof(StringBuilder), metadata.ParameterTypes.Single());
        }

        public class AllowWhitelistedParameterTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(StringBuilder sb)
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_on_unrecognized_return_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(UnrecognizedReturnTypeTestHandler),
                    },
                });
            });
        }

        public class UnrecognizedReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public StringBuilder RpcMethod(SimpleRequest1 req)
            {
                return null;
            }
        }

        [Fact]
        public void Ctor_throws_when_multiple_handlers_are_bound_to_the_same_request_type()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(MultipleHandlersBoundToTheSameRequestTypeTestHandler),
                    },
                });
            });
        }

        public class MultipleHandlersBoundToTheSameRequestTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }

            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod2()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Metadata_entry_has_class_level_supplemental_attributes()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ClassLevelSupplementalAttributesHandler),
                },
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Single(metadata.SupplementalAttributes);

            var attribute = (TestSupplementalAttribute) metadata.SupplementalAttributes.Single().Value;

            Assert.Equal(123, attribute.Value);
        }

        [TestSupplemental(123)]
        public class ClassLevelSupplementalAttributesHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Metadata_entry_has_method_level_supplemental_attributes()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MethodLevelSupplementalAttributesHandler),
                },
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Single(metadata.SupplementalAttributes);

            var attribute = (TestSupplementalAttribute) metadata.SupplementalAttributes.Single().Value;

            Assert.Equal(456, attribute.Value);
        }

        public class MethodLevelSupplementalAttributesHandler
        {
            [TestSupplemental(456)]
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Metadata_entry_method_level_supplemental_attributes_takes_priority()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesTestHandler),
                },
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Single(metadata.SupplementalAttributes);

            var attribute = (TestSupplementalAttribute) metadata.SupplementalAttributes.Single().Value;

            Assert.Equal(456, attribute.Value);
        }

        [TestSupplemental(123)]
        public class SupplementalAttributesTestHandler
        {
            [TestSupplemental(456)]
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod()
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        public class TestSupplementalAttribute : RpcSupplementalAttribute
        {
            public int Value { get; }

            public TestSupplementalAttribute(int value)
            {
                this.Value = value;
            }
        }

        [Fact]
        public async Task Execute_throws_on_null_message()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            await Snapshot.MatchError(async () =>
            {
                await rpcEngine.Execute(null, GetDefaultInstanceProvider());
            });
        }

        [Fact]
        public async Task Execute_throws_on_null_message_payload()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            await Snapshot.MatchError(async () =>
            {
                var rpcRequestMessage = new RpcRequestMessage
                {
                    Payload = null,
                };

                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
        }

        [Fact]
        public async Task Execute_throws_on_null_message_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            await Snapshot.MatchError(async () =>
            {
                var rpcRequestMessage = new RpcRequestMessage
                {
                    Payload = new ExecutorTestRequest(),
                    Type = null,
                };

                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
        }

        [Fact]
        public async Task Execute_throws_when_the_request_type_does_not_match()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            await Snapshot.MatchError(async () =>
            {
                var rpcRequestMessage = new RpcRequestMessage
                {
                    Payload = new SimpleRequest1(),
                    Type = nameof(ExecutorTestRequest),
                };

                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
        }

        [Fact]
        public async Task Execute_throws_when_no_handler_found_for_request_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            await Snapshot.MatchError(async () =>
            {
                var rpcRequestMessage = new RpcRequestMessage
                {
                    Payload = new NonRegisteredRequest(),
                    Type = nameof(NonRegisteredRequest),
                };

                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
        }

        [Fact]
        public async Task Execute_runs_the_handler_method()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            ExecutorTestHandler.RunCount = 0;

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest(),
                Type = nameof(ExecutorTestRequest),
            };

            await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            Assert.Equal(1, ExecutorTestHandler.RunCount);
        }

        [Fact]
        public async Task Execute_passes_the_request_to_the_handler_method()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest(),
                Type = nameof(ExecutorTestRequest),
            };

            await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            Assert.Equal(rpcRequestMessage.Payload, ExecutorTestHandler.Request);
        }

        [Fact]
        public async Task Execute_returns_the_response_returned_from_the_handler_method()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler),
                },
            });

            var requestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest
                {
                    Number = 42,
                },
                Type = nameof(ExecutorTestRequest),
            };

            var result = await rpcEngine.Execute(requestMessage, GetDefaultInstanceProvider());

            Assert.Equal(result.Payload, ExecutorTestHandler.Response);

            var payload = (ExecutorTestResponse) result.Payload;

            Assert.Equal(43, payload.Number);
        }

        [Fact]
        public async Task Execute_throws_when_the_handler_throws()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ThrowingExecutorTestHandler),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest(),
                Type = nameof(ExecutorTestRequest),
            };

            await Snapshot.MatchError(async () =>
            {
                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
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
            public static int RunCount;

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
        public void Ctor_allows_omitting_middleware_definition()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareTestHandler),
                },
                MiddlewareTypes = null,
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Empty(metadata.MiddlewareTypes);
        }

        [Fact]
        public void Ctor_allows_empty_array_middleware_definitions()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareTestHandler),
                },
                MiddlewareTypes = Array.Empty<Type>(),
            });

            Assert.Single(rpcEngine.Metadata);
            var metadata = rpcEngine.Metadata.Single();

            Assert.Empty(metadata.MiddlewareTypes);
        }

        public class MiddlewareTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(SimpleRequest1 req)
            {
                return Task.FromResult(new SimpleResponse1());
            }
        }

        [Fact]
        public void Ctor_throws_when_middleware_does_not_implement_RpcMiddleware()
        {
            Snapshot.MatchError(() =>
            {
                _ = new RpcEngine(new RpcEngineOptions
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(MiddlewareTestHandler),
                    },
                    MiddlewareTypes = new[]
                    {
                        typeof(NonConformingMiddleware),
                    },
                });
            });
        }

        public class NonConformingMiddleware { }

        [Fact]
        public async Task Execute_calls_middleware_in_the_correct_order()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareOrderTestHandler),
                },
                MiddlewareTypes = new[]
                {
                    typeof(MiddlewareOrderTestMiddleware1),
                    typeof(MiddlewareOrderTestMiddleware2),
                    typeof(MiddlewareOrderTestMiddleware3),
                },
            });

            var request = new MiddlewareTestRequest();

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = request,
                Type = nameof(MiddlewareTestRequest),
            };

            await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            Snapshot.Match(request.Trace);
        }

        public class MiddlewareOrderTestHandler
        {
            [RpcBind(typeof(MiddlewareTestRequest), typeof(MiddlewareTestResponse))]
            public Task<MiddlewareTestResponse> RpcMethod(MiddlewareTestRequest request)
            {
                request.Trace.Add(this.GetType().Name + "_HandlerMethod");

                return Task.FromResult(new MiddlewareTestResponse());
            }
        }

        public class MiddlewareTestRequest
        {
            public List<string> Trace { get; } = new List<string>();
        }

        public class MiddlewareTestResponse { }

        public class MiddlewareOrderTestMiddleware1 : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var request = (MiddlewareTestRequest) context.RequestMessage.Payload;

                request.Trace.Add(this.GetType().Name + "_Before");

                await next(context, instanceProvider);

                request.Trace.Add(this.GetType().Name + "_After");
            }
        }

        public class MiddlewareOrderTestMiddleware2 : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var request = (MiddlewareTestRequest) context.RequestMessage.Payload;

                request.Trace.Add(this.GetType().Name + "_Before");

                await next(context, instanceProvider);

                request.Trace.Add(this.GetType().Name + "_After");
            }
        }

        public class MiddlewareOrderTestMiddleware3 : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var request = (MiddlewareTestRequest) context.RequestMessage.Payload;

                request.Trace.Add(this.GetType().Name + "_Before");

                await next(context, instanceProvider);

                request.Trace.Add(this.GetType().Name + "_After");
            }
        }

        [Fact]
        public async Task Execute_additional_handler_arguments_are_supplied_from_the_parameters_dictionary()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(AdditionalArgumentsTestHandler),
                },
                MiddlewareTypes = new[]
                {
                    typeof(AdditionalArgumentsMiddleware),
                },
                ParameterTypeWhitelist = new[]
                {
                    typeof(AdditionalArgumentModel),
                },
            });

            var inArg = new AdditionalArgumentModel();
            AdditionalArgumentsMiddleware.AdditionalArg = inArg;

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new SimpleRequest1(),
                Type = nameof(SimpleRequest1),
            };

            await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            var outArg = AdditionalArgumentsTestHandler.AdditionalArg;

            Assert.Equal(inArg, outArg);
        }

        public class AdditionalArgumentsTestHandler
        {
            public static AdditionalArgumentModel AdditionalArg { get; set; }

            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(SimpleRequest1 request, AdditionalArgumentModel additionalArgument)
            {
                AdditionalArg = additionalArgument;

                return Task.FromResult(new SimpleResponse1());
            }
        }

        public class AdditionalArgumentsMiddleware : RpcMiddleware
        {
            public static AdditionalArgumentModel AdditionalArg { get; set; }

            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                context.SetHandlerArgument(AdditionalArg);

                await next(context, instanceProvider);
            }
        }

        public class AdditionalArgumentModel { }

        [Fact]
        public async Task Execute_correctly_returns_result_of_response_type()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResultOfResponseTypeTestHandler),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new SimpleRequest1(),
                Type = nameof(SimpleRequest1),
            };

            var result = await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            Assert.Equal(ResultOfResponseTypeTestHandler.ResultValue.Payload, result.Payload);
        }

        public class ResultOfResponseTypeTestHandler
        {
            public static RpcResult<SimpleResponse1> ResultValue { get; set; }

            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<RpcResult<SimpleResponse1>> RpcMethod(SimpleRequest1 request)
            {
                ResultValue = RpcResult.Ok(new SimpleResponse1());
                return Task.FromResult(ResultValue);
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
                MiddlewareTypes = new[]
                {
                    typeof(SimpleResultMiddleware),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new SimpleRequest1(),
                Type = nameof(SimpleRequest1),
            };

            var result = await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            Assert.Equal("test123", result.ErrorMessages[0]);
        }

        public class SimpleResultMiddleware : RpcMiddleware
        {
            public Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var result = RpcResult.Error("test123");

                context.SetResponse(result);

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Execute_throws_when_the_response_task_is_null()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResultOfResponseTypeTestHandler),
                },
                MiddlewareTypes = new[]
                {
                    typeof(NullResponseTaskMiddleware),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new SimpleRequest1(),
                Type = nameof(SimpleRequest1),
            };

            await Snapshot.MatchError(async () =>
            {
                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
        }

        public class NullResponseTaskMiddleware : RpcMiddleware
        {
            public Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                // do nothing, context.responseTask stays null
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Execute_throws_when_the_response_is_a_null_wrapped_in_a_task()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(NullWrappedInATaskTestHandler),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new SimpleRequest1(),
                Type = nameof(SimpleRequest1),
            };

            await Snapshot.MatchError(async () =>
            {
                await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());
            });
        }

        public class NullWrappedInATaskTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(SimpleRequest1 request)
            {
                return Task.FromResult<SimpleResponse1>(null);
            }
        }

        [Fact]
        public async Task Execute_can_return_task_of_response()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ObjectTaskOfResponseTestHandler),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest(),
                Type = nameof(ExecutorTestRequest),
            };

            var result = await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            var response = (ExecutorTestResponse) result.Payload;

            Assert.Equal(123, response.Number);
        }

        public class ObjectTaskOfResponseTestHandler
        {
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<ExecutorTestResponse> RpcMethod1(ExecutorTestRequest req)
            {
                return Task.FromResult(new ExecutorTestResponse
                {
                    Number = 123,
                });
            }
        }

        [Fact]
        public async Task Execute_can_return_task_of_result_of_response()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ObjectTaskOfResultOfResponseTestHandler),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest(),
                Type = nameof(ExecutorTestRequest),
            };

            var result = await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            var response = (ExecutorTestResponse) result.Payload;

            Assert.Equal(123, response.Number);
        }

        public class ObjectTaskOfResultOfResponseTestHandler
        {
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<RpcResult<ExecutorTestResponse>> RpcMethod1(ExecutorTestRequest req)
            {
                return Task.FromResult(RpcResult.Ok(new ExecutorTestResponse
                {
                    Number = 123,
                }));
            }
        }

        [Fact]
        public async Task Execute_can_return_task_of_result()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ObjectTaskOfResultTestHandler),
                },
                MiddlewareTypes = new[]
                {
                    typeof(ObjectTaskOfResultMiddleware),
                },
            });

            var rpcRequestMessage = new RpcRequestMessage
            {
                Payload = new ExecutorTestRequest(),
                Type = nameof(ExecutorTestRequest),
            };

            var result = await rpcEngine.Execute(rpcRequestMessage, GetDefaultInstanceProvider());

            Assert.Equal("test123", result.ErrorMessages[0]);
        }

        public class ObjectTaskOfResultTestHandler
        {
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<RpcResult<ExecutorTestResponse>> RpcMethod1(ExecutorTestRequest req)
            {
                return Task.FromResult(RpcResult.Ok(new ExecutorTestResponse
                {
                    Number = 123,
                }));
            }
        }

        public class ObjectTaskOfResultMiddleware : RpcMiddleware
        {
            public Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                var result = RpcResult.Error("test123");

                context.SetResponse(result);

                return Task.CompletedTask;
            }
        }

        [Fact]
        public Task GetMetadataByRequestName_returns_correct_metadata()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(GetMetadataByRequestNameTestHandler),
                },
            });

            var metadata = rpcEngine.GetMetadataByRequestName(nameof(ExecutorTestRequest));

            Assert.Equal(rpcEngine.Metadata.First(), metadata);

            return Task.CompletedTask;
        }

        [Fact]
        public Task GetMetadataByRequestName_returns_null_on_unknown_request_name()
        {
            var rpcEngine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(GetMetadataByRequestNameTestHandler),
                },
            });

            var metadata = rpcEngine.GetMetadataByRequestName(nameof(NonRegisteredRequest));

            Assert.Null(metadata);

            return Task.CompletedTask;
        }

        public class GetMetadataByRequestNameTestHandler
        {
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<RpcResult<ExecutorTestResponse>> RpcMethod1(ExecutorTestRequest req)
            {
                return Task.FromResult(RpcResult.Ok(new ExecutorTestResponse()));
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

    public class SimpleRequest1 { }

    public class SimpleResponse1 { }

    public class ExecutorTestRequest
    {
        public int Number { get; set; }
    }

    public class ExecutorTestResponse
    {
        public int Number { get; set; }
    }

    public class NonRegisteredRequest { }

    // ReSharper disable once UnusedType.Global
    public class NonRegisteredResponse { }
}
