namespace HttpTestClient
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class Program
    {
        internal static async Task Main(string[] args)
        {
            await HttpRequestAsync();
        }

        private static async Task HttpRequestAsync()
        {
            var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.LingerState = new LingerOption(true, 0);
            clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 5000));
            await using var stream = new NetworkStream(clientSocket);

            string requestJson = "{\"type\": \"PingRequest\", \"payload\":{} }";
            byte[] requestBody = JsonSerializer.SerializeToUtf8Bytes(requestJson);
            
            var requestHeaderBuilder = new StringBuilder();
            requestHeaderBuilder.Append("POST / HTTP/1.1\r\n");
            requestHeaderBuilder.Append("Host: 127.0.0.1\r\n");
            requestHeaderBuilder.Append("Content-Length: " + requestBody.Length + "\r\n");
            requestHeaderBuilder.Append("Connection: close\r\n");
            requestHeaderBuilder.Append("\r\n");
            byte[] requestHeaderBytes = Encoding.ASCII.GetBytes(requestHeaderBuilder.ToString());
            await stream.WriteAsync(requestHeaderBytes, 0, requestHeaderBytes.Length);
            await stream.WriteAsync(requestBody, 0, requestBody.Length - 10);
            
            clientSocket.Close();
        }
    }
}
