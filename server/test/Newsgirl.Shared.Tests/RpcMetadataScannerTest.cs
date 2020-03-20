using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Newsgirl.Testing;

namespace Newsgirl.Shared.Tests
{
    public class RpcMetadataScannerTest
    {
        [Fact]
        public void ScanType_returns_metadata_for_marked_types()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(RpcMarkingTest1),
                typeof(RpcMarkingTest2),
                typeof(RpcMarkingTest3),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
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
        public void ScanType_throws_when_static_method_is_marked()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(StaticRpcMethodHandler),
            };

            Snapshot.MatchError(() =>
            {
                scanner.ScanTypes(testTypes);
            });
        }
        
        public class StaticRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public static Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void ScanType_throws_when_private_method_is_marked()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(PrivateRpcMethodHandler),
            };

            Snapshot.MatchError(() =>
            {
                scanner.ScanTypes(testTypes);
            });
        }
            
        public class PrivateRpcMethodHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            private Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void ScanType_metadata_has_correct_request_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(RequestTypeTestHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(SimpleRequest1), metadata.RequestType);
        }
        
        public class RequestTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }

        [Fact]
        public void ScanType_metadata_has_correct_response_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(ResponseTypeTestHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(SimpleResponse1), metadata.ResponseType);
        }
        
        public class ResponseTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod() => Task.CompletedTask;
        }
        
        [Fact]
        public void ScanType_metadata_has_works_with_void_return_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(VoidReturnTypeTestHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(Task), metadata.ReturnType);
        }
        
        public class VoidReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(SimpleRequest1 req) => null;
        }
        
        [Fact]
        public void ScanType_metadata_has_works_with_response_return_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(ResponseReturnTypeTestHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(Task<SimpleResponse1>), metadata.ReturnType);
        }
        
        public class ResponseReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<SimpleResponse1> RpcMethod(SimpleRequest1 req) => null;
        }

        [Fact]
        public void ScanType_throws_on_invalid_parameter_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(InvalidParameterTestHandler),
            };

            Snapshot.MatchError(() =>
            {
                scanner.ScanTypes(testTypes);
            });
        }
        
        public class InvalidParameterTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(StringBuilder sb) => Task.CompletedTask;
        }
        
        [Fact]
        public void ScanType_works_when_parameter_type_is_explicitly_allowed()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(ExplicitlyAllowParameterTypeTestHandler),
            };

            var metadataList = scanner.ScanTypes(testTypes, new []
            {
                typeof(StringBuilder)
            });

            var metadata = metadataList.Single();
            
            Assert.Equal(typeof(StringBuilder), metadata.Parameters.Single());
        }
        
        public class ExplicitlyAllowParameterTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task RpcMethod(StringBuilder sb) => Task.CompletedTask;
        }
        
        [Fact]
        public void ScanType_works_when_return_type_is_wrapped_in_result()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(ResultWrappedReturnTypeTestHandler),
            };

            var metadataList = scanner.ScanTypes(testTypes, new []
            {
                typeof(StringBuilder)
            });

            var metadata = metadataList.Single();
            
            Assert.Equal(typeof(Task<Result<SimpleResponse1>>), metadata.ReturnType);
            Assert.Equal(typeof(SimpleResponse1), metadata.UnderlyingReturnType);
            Assert.True(metadata.ReturnTypeIsResultType);
            
        }
        
        public class ResultWrappedReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<Result<SimpleResponse1>> RpcMethod(StringBuilder sb) => Task.FromResult(Result.Ok(new SimpleResponse1()));
        }
        
        [Fact]
        public void ScanType_works_when_return_type_is_result_without_parameters()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(ResultWithoutParametersReturnTypeTestHandler),
            };

            var metadataList = scanner.ScanTypes(testTypes, new []
            {
                typeof(StringBuilder)
            });

            var metadata = metadataList.Single();
            
            Assert.Equal(typeof(Task<Result>), metadata.ReturnType);
            Assert.Equal(typeof(void), metadata.UnderlyingReturnType);
            Assert.True(metadata.ReturnTypeIsResultType);
            
        }
        
        public class ResultWithoutParametersReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<Result> RpcMethod(StringBuilder sb) => Task.FromResult(Result.Ok());
        }
        
        [Fact]
        public void ScanType_throws_on_invalid_return_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(InvalidReturnTypeTestHandler),
            };

            Snapshot.MatchError(() =>
            {
                scanner.ScanTypes(testTypes);
            });
        }
        
        public class InvalidReturnTypeTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public StringBuilder RpcMethod(SimpleRequest1 req) => null;
        }

        [Fact]
        public void ScanType_throws_on_invalid_return_type_as_task()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(InvalidReturnTypeAsTaskTestHandler),
            };

            Snapshot.MatchError(() =>
            {
                scanner.ScanTypes(testTypes);
            });
        }
        
        public class InvalidReturnTypeAsTaskTestHandler
        {
            [RpcBind(typeof(SimpleRequest1), typeof(SimpleResponse1))]
            public Task<StringBuilder> RpcMethod(SimpleRequest1 req) => null;
        }

        [Fact]
        public void ScanType_throws_on_colliding_requests()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(CollidingRequestsTestHandler),
            };

            Snapshot.MatchError(() =>
            {
                scanner.ScanTypes(testTypes);
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
        public void ScanType_metadata_has_supplemental_attributes()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(SupplementalAttributesTestHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();

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
        public void ScanType_metadata_has_supplemental_attributes_class_only()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(SupplementalAttributesClassOnlyHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();

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
        public void ScanType_metadata_has_supplemental_attributes_method_only()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(SupplementalAttributesMethodOnlyHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();

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
    }
    
    public class SimpleRequest1 {}
    
    public class SimpleResponse1 {}
}
