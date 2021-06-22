namespace Newsgirl.Fetcher
{
    using System.Buffers;
    using System.Collections.Generic;
    using CodeHollow.FeedReader;
    using Microsoft.Toolkit.HighPerformance.Buffers;
    using Shared;
    using Shared.Logging;

    public class FeedParser : IFeedParser
    {
        private readonly DateTimeService dateTimeService;
        private readonly Log log;

        public FeedParser(DateTimeService dateTimeService, Log log)
        {
            this.dateTimeService = dateTimeService;
            this.log = log;
        }

        public ParsedFeed Parse(string feedContent, int feedID)
        {
            var materializedFeed = FeedReader.ReadFromString(feedContent);

            var feedItems = (List<FeedItem>) materializedFeed.Items;

            var parsedFeed = new ParsedFeed(feedItems.Count);

            using (var bufferWriter = new ArrayPoolBufferWriter<byte>(feedItems.Count * 8))
            {
                for (int i = feedItems.Count - 1; i >= 0; i--)
                {
                    var feedItem = feedItems[i];

                    string feedItemStringID = GetItemStringID(feedItem, feedID);

                    if (feedItemStringID == null)
                    {
                        this.log.General(() => new LogData("Cannot ID feed item.")
                        {
                            {"feedItemJson", JsonHelper.Serialize(feedItem)},
                        });

                        continue;
                    }

                    var feedItemStringIDBytes = EncodingHelper.UTF8.GetBytes(feedItemStringID);

                    long feedItemStringIDHash = HashHelper.ComputeXx64Hash(feedItemStringIDBytes);

                    if (!parsedFeed.FeedItemHashes.Add(feedItemStringIDHash))
                    {
                        this.log.General(() => new LogData("Feed item already added.")
                        {
                            {"stringID", feedItemStringID},
                        });

                        continue;
                    }

                    parsedFeed.Items.Add(new FeedItemPoco
                    {
                        FeedItemUrl = GetItemUrl(feedItem).SomethingOrNull()?.Trim(),
                        FeedItemTitle = feedItem.Title.SomethingOrNull()?.Trim(),
                        FeedItemDescription = feedItem.Description.SomethingOrNull()?.Trim(),
                        FeedItemAddedTime = this.dateTimeService.EventTime(),
                        FeedItemStringID = feedItemStringID,
                        FeedItemStringIDHash = feedItemStringIDHash,
                    });

                    bufferWriter.Write(feedItemStringIDBytes);
                }

                parsedFeed.FeedItemsHash = HashHelper.ComputeXx64Hash(bufferWriter.WrittenSpan);
            }

            return parsedFeed;
        }

        private static string GetItemStringID(FeedItem feedItem, int feedID)
        {
            string idValue = feedItem.Id?.Trim();

            if (!string.IsNullOrWhiteSpace(idValue))
            {
                return $"FEED_ID({feedID}):ITEM_ID({idValue})";
            }

            string linkValue = feedItem.Link?.Trim();

            if (!string.IsNullOrWhiteSpace(linkValue))
            {
                return $"FEED_ID({feedID}):ITEM_LINK({linkValue})";
            }

            string titleValue = feedItem.Title?.Trim();

            if (!string.IsNullOrWhiteSpace(titleValue))
            {
                return $"FEED_ID({feedID}):ITEM_TITLE({titleValue})";
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
        ParsedFeed Parse(string feedContent, int feedID);
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

        public long FeedItemsHash { get; set; }
    }
}
