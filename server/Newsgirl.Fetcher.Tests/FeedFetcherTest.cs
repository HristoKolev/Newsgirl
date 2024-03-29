namespace Newsgirl.Fetcher.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared;
using Xdxd.DotNet.Shared;
using Xdxd.DotNet.Testing;
using Xunit;

public class FeedFetcherTest
{
    [Fact]
    public async Task Returns_Correct_Result_Parallel()
    {
        var feeds = new[]
        {
            // This one will be ignored because of matching FeedContentHash.
            new FeedPoco { FeedUrl = "fetcher-1.xml", FeedID = 1, FeedItemsHash = 0, FeedContentHash = -8357694656887908712 },

            // This one will be ignored because of matching FeedItemsHash.
            new FeedPoco { FeedUrl = "fetcher-2.xml", FeedID = 2, FeedItemsHash = 1864173257939033016, FeedContentHash = 0 },

            // This one will be processed.
            new FeedPoco { FeedUrl = "fetcher-3.xml", FeedID = 3 },
        };

        var importService = Substitute.For<IFeedItemsImportService>();
        importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));
        importService.GetMissingFeedItems(default, default).ReturnsForAnyArgs(info =>
        {
            var arr = info.Arg<long[]>();
            return arr.Skip(arr.Length / 2).ToArray();
        });

        var updates = new List<FeedUpdateModel>();

        await importService.ApplyUpdate(Arg.Do<FeedUpdateModel>(x => updates.Add(x)));

        var fetcher = new FeedFetcher(
            TestResourceContentProvider,
            new FeedParser(StubHelper.DateTimeServiceStub, StubHelper.LogStub),
            importService,
            StubHelper.DateTimeServiceStub,
            StubHelper.ErrorReporterStub,
            new DbTxServiceStub()
        );

        await fetcher.FetchFeeds();

        Snapshot.Match(updates);
    }

    [Fact]
    public async Task Reports_When_The_Content_Provider_Throws()
    {
        var feeds = new[]
        {
            new FeedPoco { FeedUrl = "fetcher-1.xml", FeedID = 1, FeedItemsHash = 351563459839931092, FeedContentHash = 123 },
        };

        var importService = Substitute.For<IFeedItemsImportService>();
        importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));

        var contentProvider = Substitute.For<IFeedContentProvider>();
        contentProvider.GetFeedContent(null).ThrowsForAnyArgs(new ApplicationException());

        var log = new StructuredLogMock();

        var errorReporter = new ErrorReporterMock();

        var fetcher = new FeedFetcher(
            contentProvider,
            new FeedParser(StubHelper.DateTimeServiceStub, log),
            importService,
            StubHelper.DateTimeServiceStub,
            errorReporter,
            new DbTxServiceStub()
        );

        await fetcher.FetchFeeds();

        Snapshot.MatchError(errorReporter.SingleException);
    }

    [Fact]
    public async Task Reports_When_The_Parser_Throws()
    {
        var feeds = new[]
        {
            new FeedPoco { FeedUrl = "fetcher-1.xml", FeedID = 1, FeedItemsHash = 351563459839931092, FeedContentHash = 123 },
        };

        var importService = Substitute.For<IFeedItemsImportService>();
        importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));

        var errorReporter = new ErrorReporterMock();

        var feedParser = Substitute.For<IFeedParser>();
        feedParser.Parse(null, 1).ThrowsForAnyArgs(new ApplicationException());

        var fetcher = new FeedFetcher(
            TestResourceContentProvider,
            feedParser,
            importService,
            StubHelper.DateTimeServiceStub,
            errorReporter,
            new DbTxServiceStub()
        );

        await fetcher.FetchFeeds();

        Snapshot.MatchError(errorReporter.SingleException);
    }

    [Fact]
    public async Task Reports_When_The_Content_Provider_Returns_Invalid_Utf8()
    {
        var feeds = new[]
        {
            new FeedPoco { FeedItemsHash = 0, FeedContentHash = 0 },
        };

        var importService = Substitute.For<IFeedItemsImportService>();
        importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));

        var errorReporter = new ErrorReporterMock();

        var feedParser = Substitute.For<IFeedParser>();
        feedParser.Parse(null, 1).ThrowsForAnyArgs(new ApplicationException());

        var invalidUtf8 = await TestHelper.GetResourceBytes("app-vnd.flatpak-icon.png");

        var contentProvider = Substitute.For<IFeedContentProvider>();
        contentProvider.GetFeedContent(null).ReturnsForAnyArgs(invalidUtf8);

        var fetcher = new FeedFetcher(
            contentProvider,
            feedParser,
            importService,
            StubHelper.DateTimeServiceStub,
            errorReporter,
            new DbTxServiceStub()
        );

        await fetcher.FetchFeeds();

        Snapshot.MatchError(errorReporter.SingleException);
    }

    private static IFeedContentProvider TestResourceContentProvider
    {
        get
        {
            var contentProvider = Substitute.For<IFeedContentProvider>();
            contentProvider.GetFeedContent(null)
                .ReturnsForAnyArgs(info =>
                {
                    var feedPoco = info.Arg<FeedPoco>();
                    var contentTask = TestHelper.GetResourceText(feedPoco.FeedUrl);

                    return contentTask.ContinueWith(x => EncodingHelper.UTF8.GetBytes(x.Result));
                });

            return contentProvider;
        }
    }
}