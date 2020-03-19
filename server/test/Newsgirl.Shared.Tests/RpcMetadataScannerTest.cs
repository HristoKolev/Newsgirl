using System;
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
                typeof(Test1RpcType1),
                typeof(Test1RpcType2),
                typeof(Test1RpcType3),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(Test1RpcType3), metadata.HandlerClass);

            var methodInfos = typeof(Test1RpcType3).GetMethods().Where(x => x.DeclaringType == typeof(Test1RpcType3)).ToArray();
            
            Assert.Equal(methodInfos.Single(), metadata.HandlerMethod);
        }

        [Fact]
        public void ScanType_throws_when_static_method_is_marked()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(StaticRpcMethodHandler),
            };

            Exception exception = null;

            try
            {
                scanner.ScanTypes(testTypes);
            }
            catch (Exception err)
            {
                exception = err;
            }
            
            Snapshot.MatchError(exception);
        }
        
        [Fact]
        public void ScanType_throws_when_private_method_is_marked()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(PrivateRpcMethodHandler),
            };

            Exception exception = null;

            try
            {
                scanner.ScanTypes(testTypes);
            }
            catch (Exception err)
            {
                exception = err;
            }
            
            Snapshot.MatchError(exception);
        }
        
        [Fact]
        public void ScanType_metadata_has_correct_request_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(Test1RpcType3),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(Test1SimpleRpcRequest), metadata.RequestType);
        }
        
        [Fact]
        public void ScanType_metadata_has_correct_response_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(Test1RpcType3),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();
            
            Assert.Equal(typeof(Test1SimpleRpcResponse), metadata.ResponseType);
        }
        
        [Fact]
        public void ScanType_throws_on_invalid_parameter_type()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(RpcHandlerWithInvalidParameter),
            };

            Exception exception = null;

            try
            {
                scanner.ScanTypes(testTypes);
            }
            catch (Exception err)
            {
                exception = err;
            }
            
            Snapshot.MatchError(exception);
        }
        
        [Fact]
        public void ScanType_throws_on_colliding_requests()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(RpcHandlerWithCollidingRequests),
            };

            Exception exception = null;

            try
            {
                scanner.ScanTypes(testTypes);
            }
            catch (Exception err)
            {
                exception = err;
            }
            
            Snapshot.MatchError(exception);
        }

        [Fact]
        public void ScanType_metadata_has_supplemental_attributes()
        {
            var scanner = new RpcMetadataScanner();

            var testTypes = new[]
            {
                typeof(SupplementalAttributesHandler),
            };
            
            var handlerMetadataList = scanner.ScanTypes(testTypes);

            Assert.Single(handlerMetadataList);
            
            var metadata = handlerMetadataList.Single();

            Assert.Single(metadata.SupplementalAttributes);
            
            var attribute = metadata.SupplementalAttributes.Single().Value as TestSupplementalAttribute;
            
            Assert.Equal(456, attribute.Value);
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

    }
    
    public class Test1RpcType1 {}

    public class Test1RpcType2
    {
        public Task NonRpcMethod()
        {
            return Task.CompletedTask;
        }
    }

    public class Test1RpcType3
    {
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        public Task RpcMethod()
        {
            return Task.CompletedTask;
        }
    }
    
    public class Test1SimpleRpcRequest {}
    
    public class Test1SimpleRpcResponse {}
    
    public class StaticRpcMethodHandler
    {
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        public static Task RpcMethod()
        {
            return Task.CompletedTask;
        }   
    }
    
    public class PrivateRpcMethodHandler
    {
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        private Task RpcMethod()
        {
            return Task.CompletedTask;
        }   
    }
    
    public class RpcHandlerWithInvalidParameter
    {
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        public Task RpcMethod(StringBuilder sb)
        {
            return Task.CompletedTask;
        }   
    }
    
    public class RpcHandlerWithCollidingRequests
    {
        [RpcBind(typeof(SimpleRequest2), typeof(SimpleResponse2))]
        public Task RpcMethod()
        {
            return Task.CompletedTask;
        }
        
        [RpcBind(typeof(SimpleRequest2), typeof(SimpleResponse2))]
        public Task RpcMethod2()
        {
            return Task.CompletedTask;
        }   
    }

    public class SimpleRequest2
    {
    }

    public class SimpleResponse2
    {
    }

    [TestSupplemental(123)]
    public class SupplementalAttributesHandler
    {
        [TestSupplemental(456)]
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        public Task RpcMethod()
        {
            return Task.CompletedTask;
        }
    }
    
    [TestSupplemental(123)]
    public class SupplementalAttributesClassOnlyHandler
    {
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        public Task RpcMethod()
        {
            return Task.CompletedTask;
        }
    }
    
    public class SupplementalAttributesMethodOnlyHandler
    {
        [TestSupplemental(456)]
        [RpcBind(typeof(Test1SimpleRpcRequest), typeof(Test1SimpleRpcResponse))]
        public Task RpcMethod()
        {
            return Task.CompletedTask;
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
}
