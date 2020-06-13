namespace elk_client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var cfg = new ElasticsearchConfig
            {
                Url = "http://dev-host.lan:9200",
                Username = "newsgirl",
                Password = "test123"
            };
            
            var client = new ElasticsearchClient(cfg);

            var data = Enumerable.Range(0, 10).Select(i => new LogData("this is a test message" + i)).ToArray();

            await client.BulkIndex("newsgirl-server-general-dev", data);
        }
    }

    public class LogData : IEnumerable, ElasticsearchSerializable
    {
        public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();

        public LogData(string message)
        {
            this.Fields.Add("message", message);
            this.Fields.Add("log_date", DateTime.UtcNow.ToString("O"));
        }

        public void Add(string key, object val) => this.Fields.Add(key, val);

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        public static implicit operator LogData(string x) => new LogData(x);

        public object ToSerializableObject() => this.Fields;
    }
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ElasticsearchConfig
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    public class ElasticsearchClient
    {
        private readonly HttpClient httpClient;

        public ElasticsearchClient(HttpClient httpClient, ElasticsearchConfig config)
        {
            this.httpClient = httpClient;
            
            httpClient.Timeout = TimeSpan.FromMinutes(1);
            httpClient.BaseAddress = new Uri(config.Url);

            string basicToken = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}")
            );
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
        }

        public ElasticsearchClient(ElasticsearchConfig config) : this(new HttpClient(), config)
        {
        }

        public async Task Index(string index, byte[] body)
        {
            var content = new ByteArrayContent(body);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await this.httpClient.PostAsync(
                new Uri(index + "/_doc/", UriKind.Relative),
                content
            );

            string json = await response.Content.ReadAsStringAsync();
            
            response.EnsureSuccessStatusCode();

            Console.WriteLine(json);
        }
        
        private byte[] headerBytes = Encoding.UTF8.GetBytes("{\"create\":{}}\n");
        private byte[] newLineBytes = Encoding.UTF8.GetBytes("\n");
        
        public async Task BulkIndex<T>(string index, T[] data) where T: ElasticsearchSerializable
        {
            await using var memStream = new MemoryStream();

            for (int i = 0; i < data.Length; i++)
            {
                object item = data[i].ToSerializableObject();

                await memStream.WriteAsync(this.headerBytes);
                
                await JsonSerializer.SerializeAsync(memStream, item, item.GetType());
                
                await memStream.WriteAsync(this.newLineBytes);
            }

            memStream.Position = 0;

            var content = new StreamContent(memStream);
            
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            var response = await this.httpClient.PostAsync(
                new Uri(index + "/_bulk", UriKind.Relative),
                content
            );

            string json = await response.Content.ReadAsStringAsync();

            var model = JsonSerializer.Deserialize<ElasticsearchBulkResponse>(json, new JsonSerializerOptions(){PropertyNameCaseInsensitive = true});

            Console.WriteLine(model);
            
            response.EnsureSuccessStatusCode();

            Console.WriteLine(json);
        }
    }

    public class ElasticsearchBulkResponse
    {
        public bool Errors { get; set; } = true;
    }
    
    public interface ElasticsearchSerializable
    {
        object ToSerializableObject() => this; 
    }   
}
