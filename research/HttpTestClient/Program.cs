namespace HttpTestClient
{
    using System;
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
            string address = "http://127.0.0.1:5007";
            
            var uri = new Uri(address);

            using (var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                clientSocket.LingerState = new LingerOption(true, 0);
                clientSocket.Connect(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
                clientSocket.UseOnlyOverlappedIO = true;

                string requestJson = "{\"type\": \"PingRequest\", \"payload\":{} }";
                byte[] requestBody = JsonSerializer.SerializeToUtf8Bytes(requestJson);
            
                var requestHeaderBuilder = new StringBuilder();
                requestHeaderBuilder.Append("POST / HTTP/1.1\r\n");
                requestHeaderBuilder.Append($"Host: {uri.Host}\r\n");
                requestHeaderBuilder.Append("Content-Length: " + requestBody.Length + "\r\n");
                requestHeaderBuilder.Append("Connection: close\r\n");
                requestHeaderBuilder.Append("\r\n");
                    
                await clientSocket.SendAsync(Encoding.ASCII.GetBytes(requestHeaderBuilder.ToString()), SocketFlags.None);

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // ---------> -10 here
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                await clientSocket.SendAsync(new ReadOnlyMemory<byte>(requestBody, 0, requestBody.Length - 10), SocketFlags.None); // <--------------

                clientSocket.Close(0);
                    
                // await using (var mem = new MemoryStream())
                // {
                //     await networkStream.CopyToAsync(mem);
                //
                //     string str = EncodingHelper.UTF8.GetString(mem.GetBuffer(), 0, (int) mem.Length);
                //
                //     Console.WriteLine(str);
                // }


            }

        }
    }
}
