using System.Collections.Generic;
using System.IO;
using System.Text;

using CodeHollow.FeedReader;
using Newtonsoft.Json;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public class FeedParser : IFeedParser
    {
        private readonly IHasher hasher;
        private readonly AppConfig appConfig;

        public FeedParser(IHasher hasher, AppConfig appConfig)
        {
            this.hasher = hasher;
            this.appConfig = appConfig;
        }
        
        public ParsedFeed Parse(string feedContent)
        {
            var materializedFeed = FeedReader.ReadFromString(feedContent);
            
            var allItems = (List<FeedItem>)materializedFeed.Items;

            var parsedFeed = new ParsedFeed(allItems.Count);

            using (var memoryStream = new MemoryStream(allItems.Count * 8))
            {
                byte[] stringIDBytes;

                for (int i = allItems.Count - 1; i >= 0; i--)
                {
                    var item = allItems[i];
            
                    string stringID = GetItemStringID(item);

                    if (stringID == null)
                    {
                        if (this.appConfig.Debug.General)
                        {
                            MainLogger.Debug($"Cannot ID feed item: {JsonConvert.SerializeObject(item)}");
                        }
                    
                        continue;
                    }
            
                    stringIDBytes = Encoding.UTF8.GetBytes(stringID);
                
                    long itemHash = this.hasher.ComputeHash(stringIDBytes);

                    if (!parsedFeed.FeedItemHashes.Add(itemHash))
                    {
                        if (this.appConfig.Debug.General)
                        {
                            MainLogger.Debug($"Feed item already added: {stringID}");
                        }
                    
                        continue;
                    }

                    parsedFeed.Items.Add(new ParsedFeedItem
                    {
                        Item = item,
                        FeedItemHash = itemHash,
                    });
                
                    memoryStream.Write(stringIDBytes, 0, stringIDBytes.Length);
                }

                parsedFeed.FeedHash = this.hasher.ComputeHash(memoryStream);
            }

            return parsedFeed;
        }
        
        private static string GetItemStringID(FeedItem feedItem)
        {
            string idValue = feedItem.Id?.Trim();
            string linkValue = feedItem.Link?.Trim();
            string titleValue = feedItem.Title?.Trim();

            if (!string.IsNullOrWhiteSpace(idValue))
            {
                return $"ID({idValue})";
            }

            if (!string.IsNullOrWhiteSpace(linkValue))
            {
                return $"LINK({linkValue})";
            }

            if (!string.IsNullOrWhiteSpace(titleValue))
            {
                return $"TITLE({titleValue})";
            }

            return null;
        }
    }
    
    public interface IFeedParser
    {
        ParsedFeed Parse(string feedContent);
    }
    
    public class ParsedFeed
    {
        public ParsedFeed(int capacity)
        {
            this.Items = new List<ParsedFeedItem>(capacity);
            this.FeedItemHashes = new HashSet<long>(capacity);
        }
        
        public List<ParsedFeedItem> Items { get; } 

        public HashSet<long> FeedItemHashes { get; }
        
        public long FeedHash { get; set; }
    }


    public class ParsedFeedItem
    {
        public FeedItem Item { get; set; }

        public long FeedItemHash { get; set; }
    }
}