namespace RpcCodeGenerator
{
    using System.IO;
    using System.Linq;
    using Newsgirl.Shared;

    public class Program
    {
        public static void Main()
        {
            var engine = new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = typeof(Newsgirl.Server.Program)
                    .Assembly.ExportedTypes.ToArray(),
            });

            const string FILE_TEMPLATE = @"namespace Newsgirl.Server
{
    using System.Threading.Tasks;
    using Shared;

    public abstract class RpcClient
    {
        protected abstract Task<RpcResult<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request);

{methods}
    }
}
";
            var methods = engine.Metadata.Select(metadata =>
            {
                string methodName = metadata.RequestType.Name;

                const string REQUEST_POSTFIX = "request";

                if (methodName.ToLower().EndsWith(REQUEST_POSTFIX))
                {
                    methodName = methodName.Remove(methodName.Length - REQUEST_POSTFIX.Length, REQUEST_POSTFIX.Length);
                }

                return $"        public Task<RpcResult<{metadata.ResponseType.Name}>> " +
                       $"{methodName}({metadata.RequestType.Name} request)\n        {{\n    " +
                       $"        return this.RpcExecute<{metadata.RequestType.Name}, {metadata.ResponseType.Name}>(request);\n        }}";
            });

            string outputContents = FILE_TEMPLATE.Replace("{methods}", string.Join("\n\n", methods));

            string outputFilePath = Path.Combine(
                Path.GetDirectoryName(typeof(Program).Assembly.Location)!,
                "../../../../../../server/src/Newsgirl.Server/RpcClient.cs"
            );

            File.WriteAllText(outputFilePath, outputContents);
        }
    }
}
