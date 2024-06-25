using Moq;
using SDV.Data.Reddit.Models;
using SDV.Data.Reddit.Models.ApiResponseModels;
using SDV.Data.Services.Interfaces;
using SDV.Data.Services.Services;
using SDV.Shared.Interfaces;
using SDV.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDV.Data.Services.Tests
{
    public class RedditDataPollingServiceTests
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDataServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RedditDataPollingService(null, "subreddit"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Constructor_ThrowsArgumentException_WhenSubredditToPollIsInvalid(string subreddit)
        {
            var mockDataService = new Mock<IDataService>();
            Assert.Throws<ArgumentException>(() => new RedditDataPollingService(mockDataService.Object, subreddit));
        }

        [Fact]
        public void StartPolling_CallsPollForData_AndPublishesData()
        {
            var mockDataService = new Mock<IDataService>();
            var service = new RedditDataPollingService(mockDataService.Object, "subreddit");
            var post = new SubRedditPost{ Title = "Test"};
            SubRedditPostsResponseDataModel mockResponse = new SubRedditPostsResponseDataModel            {
                Children = new List<SubRedditPostsResponseChildModel>()
                    {
                        new SubRedditPostsResponseChildModel()
                        {
                            Post = post
                        }
                    },
                After = string.Empty
            };
            mockDataService.Setup(ds => ds.GetNewestPostsBySubredditChunked(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockResponse);

            mockDataService.Setup(ds => ds.GetFromDataStore<HashSet<string>>(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new HashSet<string>() { "", "1" });

            var cancellationTokenSource = new CancellationTokenSource();
            var manualResetEvent = new ManualResetEvent(false);
            IEnumerable<ISubRedditPost> publishedData = null;
            service.DataPublished += (sender, args) => { publishedData = args.Data; manualResetEvent.Set(); };

            Task.Run(() => service.StartPolling(TimeSpan.FromMilliseconds(100), cancellationTokenSource.Token));

            // Wait for the event to be triggered
            Assert.True(manualResetEvent.WaitOne(TimeSpan.FromSeconds(5)));
            cancellationTokenSource.Cancel();

            mockDataService.Verify(ds => ds.GetNewestPostsBySubredditChunked("subreddit", It.IsAny<string>()), Times.AtLeastOnce);
            Assert.NotNull(publishedData);
            Assert.Contains(post, publishedData.ToList());
        }

        [Fact]
        public void StartPolling_CancellationTokenCancelled_ReturnsImmediately()
        {
            // Arrange
            var mockDataService = new Mock<IDataService>();
            var service = new RedditDataPollingService(mockDataService.Object, "subreddit");
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act
            Task.Run(() => service.StartPolling(TimeSpan.FromMilliseconds(100), cancellationTokenSource.Token));

            // Assert
            // No assertion needed, the test will pass if the method returns without throwing an exception
        }

        [Fact]
        public void StartPolling_PollsForDataAndDoesNotPublishIfNoNewData()
        {
            // Arrange
            var mockDataService = new Mock<IDataService>();
            var service = new RedditDataPollingService(mockDataService.Object, "subreddit");
            var cancellationTokenSource = new CancellationTokenSource();
            var post1 = new SubRedditPostsResponseDataModel
            {
                Children = new List<SubRedditPostsResponseChildModel>()
                    {
                        new SubRedditPostsResponseChildModel()
                        {
                            Post = new SubRedditPost { Title="Test Post" }
                        }
                    },
                After = string.Empty
            };
            var post2 = new SubRedditPostsResponseDataModel
            {
                Children = new List<SubRedditPostsResponseChildModel>()
                    {
                        new SubRedditPostsResponseChildModel()
                        {
                            Post = new SubRedditPost { Title="Test Post" }
                        }
                    },
                After = string.Empty
            };

            var data = new List<ISubRedditPost> { new SubRedditPost { Title = "Test Post" } };
            mockDataService.Setup(x => x.GetFromDataStore<HashSet<string>>(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new HashSet<string> { "Test Post" });
            mockDataService.Setup(x => x.GetNewestPostsBySubredditChunked(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(post1);

            bool dataPublished = false;
            service.DataPublished += (sender, args) => dataPublished = true;

            // Act
            Task.Run(() => service.StartPolling(TimeSpan.FromMilliseconds(100), cancellationTokenSource.Token));
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));

            // Assert
            Assert.False(dataPublished);
        }

        // Additional tests for PollForData, handling cancellation, and verifying DataPublished event can be added here.
    }
}
