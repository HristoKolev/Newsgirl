using System;

namespace ElasticDemo
{
    using System.Threading.Tasks;
    using Elasticsearch.Net;

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var node = new Uri("http://192.168.0.107:9200");
                var config = new ConnectionConfiguration(node);
                config.BasicAuthentication("elastic", "changeme");
                var client = new ElasticLowLevelClient(config);

                for (int i = 0; i < 100; i++)
                {
                    var ss = await client.IndexAsync<MyModel>("hristo_logs", "{\"cat_id\": " + i + "}");

                    Console.WriteLine(ss);   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    class MyModel : ElasticsearchResponseBase
    {
    }
}
