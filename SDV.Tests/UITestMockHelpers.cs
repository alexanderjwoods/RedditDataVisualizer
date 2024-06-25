using Moq;
using SDV.Data.Reddit.Models.ApiResponseModels;
using SDV.Data.Services.Interfaces;
using SDV.Shared.Interfaces;
using SDV.Shared.Models;

namespace SDV.UITests
{
    public static class UITestMockHelpers
    {
        public static void SetUpStandardDataServiceMocks(ref Mock<IDataService> _mockDataService)
        {
            _mockDataService.Setup(x => x.GetNewestPostsBySubredditChunked(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new SubRedditPostsResponseDataModel()
                {
                    After = "",
                    Children = new List<SubRedditPostsResponseChildModel>()
                }));

            _mockDataService.Setup(service => service.GetFromDataStore<HashSet<string>>(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new HashSet<string>());

            _mockDataService.Setup(service => service.GetFromDataStore<List<ISubRedditPost>?>(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new List<ISubRedditPost>
            {
                new SubRedditPost { Title = "Test Post 1", Author = "Author1", Upvotes = 100, Downvotes = 10, NumComments = 5, Url= "Url", SynchronizedLast = DateTime.MinValue },
                new SubRedditPost { Title = "Test Post 2", Author = "Author1", Upvotes = 100, Downvotes = 10, NumComments = 5, Url= "Url", SynchronizedLast = DateTime.MinValue },
                new SubRedditPost { Title = "Mature Post", Author = "Author1", Upvotes = 100, Downvotes = 10, NumComments = 5, Url= "Url", SynchronizedLast = DateTime.MinValue, MatureContent = true},
            });
        }
    }
}
