using System.Collections.Generic;
using System.IO;
using System.Text;

using CodeHollow.FeedReader;
using Newsgirl.Shared.Data;
using Newtonsoft.Json;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public class FeedParser : IFeedParser
    {
        private readonly Hasher hasher;
        private readonly IDateProvider dateProvider;
        private readonly ILog log;

        public FeedParser(Hasher hasher, IDateProvider dateProvider, ILog log)
        {
            this.hasher = hasher;
            this.dateProvider = dateProvider;
            this.log = log;
        }
        
        public ParsedFeed Parse(string feedContent)
        {
            var materializedFeed = FeedReader.ReadFromString(feedContent);
            
            var allItems = (List<FeedItem>)materializedFeed.Items;

            var parsedFeed = new ParsedFeed(allItems.Count);

            var fetchTime = this.dateProvider.Now();
            
            using (var memoryStream = new MemoryStream(allItems.Count * 8))
            {
                byte[] stringIDBytes;

                for (int i = allItems.Count - 1; i >= 0; i--)
                {
                    var feedItem = allItems[i];
            
                    string stringID = GetItemStringID(feedItem);

                    if (stringID == null)
                    {
                        this.log.Debug($"Cannot ID feed item: {JsonConvert.SerializeObject(feedItem)}");

                        continue;
                    }
            
                    stringIDBytes = Encoding.UTF8.GetBytes(stringID);
                
                    long feedItemHash = this.hasher.ComputeHash(stringIDBytes);

                    if (!parsedFeed.FeedItemHashes.Add(feedItemHash))
                    {
                        this.log.Debug($"Feed item already added: {stringID}");
                    
                        continue;
                    }

                    parsedFeed.Items.Add(new FeedItemPoco
                    {
                        FeedItemUrl = GetItemUrl(feedItem).SomethingOrNull()?.Trim(),
                        FeedItemTitle = feedItem.Title.SomethingOrNull()?.Trim(),
                        FeedItemDescription = feedItem.Description.SomethingOrNull()?.Trim(),
                        FeedItemAddedTime = fetchTime,
                        FeedItemHash = feedItemHash,
                    });
                
                    memoryStream.Write(stringIDBytes, 0, stringIDBytes.Length);
                }

                var allBytes = memoryStream.ToArray();

                parsedFeed.FeedHash = this.hasher.ComputeHash(allBytes);
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
        
        private static string GetItemUrl(FeedItem feedItem)
        {
            string linkValue = feedItem.Link?.Trim();
            
            if (!string.IsNullOrWhiteSpace(linkValue) && linkValue.StartsWith("http"))
            {
                return linkValue;
            }

            string idValue = feedItem.Id?.Trim();
            
            if (!string.IsNullOrWhiteSpace(idValue) && idValue.StartsWith("http"))
            {
                return idValue;
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
            this.Items = new List<FeedItemPoco>(capacity);
            this.FeedItemHashes = new HashSet<long>(capacity);
        }
        
        public List<FeedItemPoco> Items { get; } 

        public HashSet<long> FeedItemHashes { get; }
        
        public long FeedHash { get; set; }
    }
}