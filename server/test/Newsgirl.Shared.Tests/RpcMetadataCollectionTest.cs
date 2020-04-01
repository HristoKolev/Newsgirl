using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Newsgirl.Testing;

namespace Newsgirl.Shared.Tests
{
    public class RpcMetadataCollectionTest
    {
        [Fact]
        public void Build_returns_metadata_for_marked_types()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(RpcMarkingTest1),
                    typeof(RpcMarkingTest2),
                    typeof(RpcMarkingTest3),
                },
            });
            
            Assert.Single(metadataCollection.Handlers);
            
            var metadata = metadataCollection.Handlers.Single();
            
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
            public Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void Build_throws_when_static_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
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
            public static Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void Build_throws_when_private_method_is_marked()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(PrivateRpcMethodHandler),
                    },
                });
            });
        }
            
        public class PrivateRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            private Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void Build_metadata_has_correct_request_type()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(RequestTypeTestHandler),
                },
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();
            
            Assert.Equal(typeof(SimpleRequest1), metadata.RequestType);
        }
        
        public class RequestTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void Build_metadata_has_correct_response_type()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResponseTypeTestHandler),
                },
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();
            
            Assert.Equal(typeof(SimpleResponse1), metadata.ResponseType);
        }
        
        public class ResponseTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }
        
        [Fact]
        public void Build_metadata_has_works_with_void_return_type()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(VoidReturnTypeTestHandler),
                },
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();
            
            Assert.Equal(typeof(Task), metadata.ReturnType);
        }
        
        public class VoidReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(SimpleRequest1 req) => null;
        }
        
        [Fact]
        public void Build_metadata_has_works_with_response_return_type()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ResponseReturnTypeTestHandler),
                },
            });

            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();
            
            Assert.Equal(typeof(Task<SimpleResponse1>), metadata.ReturnType);
        }
        
        public class ResponseReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(SimpleRequest1 req) => null;
        }

        [Fact]
        public void Build_throws_on_invalid_parameter_type()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(InvalidParameterTestHandler),
                    },
                });
            });
        }
        
        public class InvalidParameterTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(StringBuilder sb) => Task.CompletedTask;
        }
        
        [Fact]
        public void Build_works_when_parameter_type_is_explicitly_allowed()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExplicitlyAllowParameterTypeTestHandler),
                },
                HandlerArgumentTypeWhiteList = new []
                {
                    typeof(StringBuilder)
                }
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();
            
            Assert.Equal(typeof(StringBuilder), metadata.Parameters.Single());
        }
        
        public class ExplicitlyAllowParameterTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(StringBuilder sb) => Task.CompletedTask;
        }
 
        [Fact]
        public void Build_throws_on_invalid_return_type()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(InvalidReturnTypeTestHandler),
                    },
                });
            });
        }
        
        public class InvalidReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public StringBuilder RpcMethod(SimpleRequest1 req) => null;
        }

        [Fact]
        public void Build_throws_on_invalid_return_type_as_task()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(InvalidReturnTypeAsTaskTestHandler),
                    },
                });
            });
        }
        
        public class InvalidReturnTypeAsTaskTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<StringBuilder> RpcMethod(SimpleRequest1 req) => null;
        }

        [Fact]
        public void Build_throws_on_colliding_requests()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(CollidingRequestsTestHandler),
                    },
                });
            });
        }
        
        public class CollidingRequestsTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;

            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod2() => Task.CompletedTask;
        }

        [Fact]
        public void Build_metadata_has_supplemental_attributes()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesTestHandler),
                },
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();

            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(456, attribute.Value);
        }
        
        [TestSupplemental(123)]
        public class SupplementalAttributesTestHandler
        {
            [TestSupplemental(456)]
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }
        
        [Fact]
        public void Build_metadata_has_supplemental_attributes_class_only()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesClassOnlyHandler),
                },
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();
            
            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(123, attribute.Value);
        }
        
        [TestSupplemental(123)]
        public class SupplementalAttributesClassOnlyHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }
        
        [Fact]
        public void Build_metadata_has_supplemental_attributes_method_only()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(SupplementalAttributesMethodOnlyHandler),
                },
            });

            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();

            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(456, attribute.Value);
        }
        
        public class SupplementalAttributesMethodOnlyHandler
        {
            [TestSupplemental(456)]
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }
        
        public class TestSupplementalAttribute : RpcSupplementalAttribute
        {
            public int Value { get; }

            public TestSupplementalAttribute(int value) => this.Value = value;
        }
        
        [Fact]
        public void GetMetadataByRequestType_returns_the_correct_metadata()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MetadataByRequestNameTestHandler),
                },
            });

            var selectedMetadata = metadataCollection.GetMetadataByRequestType(typeof(MetadataByRequestNameRequest2));
            
            Assert.Equal(metadataCollection.Handlers[1], selectedMetadata);
        }
        
        public class MetadataByRequestNameTestHandler
        {
            [RpcBind(typeof(MetadataByRequestNameRequest1), typeof(MetadataByRequestNameResponse1))]
            public Task<MetadataByRequestNameResponse1> RpcMethod1(MetadataByRequestNameRequest1 req) => Task.FromResult(new MetadataByRequestNameResponse1());
            
            [RpcBind(typeof(MetadataByRequestNameRequest2), typeof(MetadataByRequestNameResponse2))]
            public Task<MetadataByRequestNameResponse2> RpcMethod1(MetadataByRequestNameRequest2 req) => Task.FromResult(new MetadataByRequestNameResponse2());
            
            [RpcBind(typeof(MetadataByRequestNameRequest3), typeof(MetadataByRequestNameResponse3))]
            public Task<MetadataByRequestNameResponse3> RpcMethod1(MetadataByRequestNameRequest3 req) => Task.FromResult(new MetadataByRequestNameResponse3());
        }
        
        [Fact]
        public void Build_works_correctly_with_null_middleware_param()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareTestHandler),
                },
                MiddlewareTypes = null,
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();

            Assert.Empty(metadata.MiddlewareTypes);
        }
        
        [Fact]
        public void Build_works_correctly_with_empty_middleware_param()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(MiddlewareTestHandler),
                },
                MiddlewareTypes = Array.Empty<Type>(),
            });
            
            Assert.Single(metadataCollection.Handlers);
            var metadata = metadataCollection.Handlers.Single();

            Assert.Empty(metadata.MiddlewareTypes);
        }
        
        public class MiddlewareTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(SimpleRequest1 req) => Task.CompletedTask;
        }
        
        [Fact]
        public void Build_throws_when_middleware_does_not_implement_the_interface()
        {
            Snapshot.MatchError(() =>
            {
                RpcMetadataCollection.Build(new RpcMetadataBuildParams
                {
                    PotentialHandlerTypes = new[]
                    {
                        typeof(MiddlewareTestHandler),
                    },
                    MiddlewareTypes = new []
                    {
                        typeof(NonConformingMiddleware)
                    }
                });
            });
        }
        
        public class NonConformingMiddleware
        {
        }
    }
    
    public class SimpleRequest1 {}
    
    public class SimpleResponse1 {}

    public class MetadataByRequestNameRequest1{}
    
    public class MetadataByRequestNameResponse1{}
    
    public class MetadataByRequestNameRequest2{}
    
    public class MetadataByRequestNameResponse2{}
    
    public class MetadataByRequestNameRequest3{}
    
    public class MetadataByRequestNameResponse3{}
}
