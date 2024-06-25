using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using SDV.Data.Interfaces;
using SDV.Data.Reddit.Interfaces;
using SDV.Data.Reddit.Models;
using SDV.Data.Reddit.Models.ApiResponseModels;
using SDV.Data.RedditApi.Models.ApiResponseModels;
using SDV.Data.Services.Services;
using System.Net;
using System.Reflection;

namespace SDV.Data.Services.Tests
{
    public class RedditDataServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration = new();
        private readonly Mock<IDataStore> _mockDataStore = new();
        private readonly Mock<IRedditApiClient> _mockRedditApiClient = new();
        private readonly Mock<IMemoryCache> _mockMemoryCache = new();
        private readonly RedditDataService _redditDataService;

        public RedditDataServiceTests()
        {
            _mockConfiguration.Setup(c => c["Secrets:client_id"]).Returns("testClientId");
            _mockConfiguration.Setup(c => c["Secrets:client_secret"]).Returns("testClientSecret");

            _redditDataService = new RedditDataService(_mockConfiguration.Object, _mockDataStore.Object, _mockRedditApiClient.Object, _mockMemoryCache.Object);
        }

        [Fact]
        public async Task Authenticate_Success()
        {
            // Arrange
            var mockResponse = new RedditApiResponseWrapper<BearerToken>(
                HttpStatusCode.OK,
                new BearerToken("testToken", "", 60, ""));
            _mockRedditApiClient.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockResponse);

            // Act
            var result = await _redditDataService.Authenticate();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Authenticate_HttpStatusCode400_ThrowsHttpRequestException()
        {
            // Arrange
            var mockResponse = new RedditApiResponseWrapper<BearerToken>(
                HttpStatusCode.BadRequest);
            _mockRedditApiClient.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockResponse);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _redditDataService.Authenticate());
        }

        [Theory]
        [InlineData("")]
        [InlineData("     ")]
        public async Task Authenticate_OKStatusCodeAndEmptyAccessToken_InvalidOperationException(string token)
        {
            // Arrange
            var mockResponse = new RedditApiResponseWrapper<BearerToken>(
                HttpStatusCode.OK, new BearerToken(token, "", 60, ""), "");
            _mockRedditApiClient.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockResponse);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _redditDataService.Authenticate());
        }

        [Fact]
        public async Task Authenticate_OKStatusCodeAndNullValue_InvalidOperationException()
        {
            // Arrange
            var mockResponse = new RedditApiResponseWrapper<BearerToken>(
                HttpStatusCode.OK, null, "");
            _mockRedditApiClient.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockResponse);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _redditDataService.Authenticate());
        }

        [Fact]
        public async Task GetNewestPostsBySubredditChunked_Success()
        {
            // Arrange
            var mockToken = new BearerToken("testToken", "", 60, "");
            var mockResponse = new RedditApiResponseWrapper<SubRedditPostsResponse>(
                HttpStatusCode.OK,
                new SubRedditPostsResponse { Data = new SubRedditPostsResponseDataModel() });

            _mockRedditApiClient.Setup(x => x.GetNewPostsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockResponse);
            _redditDataService.GetType().GetField("_bearerToken", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, mockToken);

            // Act
            var result = await _redditDataService.GetNewestPostsBySubredditChunked("testSubreddit", "after");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetNewestPostsBySubredditChunked_Failure_ThrowsException()
        {
            // Arrange
            var mockToken = new BearerToken("testToken", "", 60, "");
            var mockResponse = new RedditApiResponseWrapper<SubRedditPostsResponse>(
                HttpStatusCode.BadRequest,
                new SubRedditPostsResponse { Data = new SubRedditPostsResponseDataModel() });

            _mockRedditApiClient.Setup(x => x.GetNewPostsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockResponse);
            _redditDataService.GetType().GetField("_bearerToken", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, mockToken);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _redditDataService.GetNewestPostsBySubredditChunked("testSubreddit", "after"));
        }

        [Fact]
        public async Task SaveToDataStore_WithoutCacheExpiration_BypassesCache()
        {
            // Arrange
            string testKey = "testKey";
            string testValue = "testValue";

            // Act
            await _redditDataService.SaveToDataStore(testKey, testValue);

            // Assert
            _mockMemoryCache.Verify(x => x.CreateEntry(testKey), Times.Never);
        }

        [Fact]
        public async Task SaveToDataStore_WithCacheExpiration_SetsDataInCache()
        {
            // Arrange
            string testKey = "testKey";
            string testValue = "testValue";

            // Act
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            _mockMemoryCache.Setup(x => x.CreateEntry(testKey)).Returns(Mock.Of<ICacheEntry>());
            await _redditDataService.SaveToDataStore(testKey, testValue, TimeSpan.FromMinutes(1));

            // Assert
            _mockMemoryCache.Verify(x => x.CreateEntry(testKey), Times.Once);
        }

        [Fact]
        public async Task SaveToDataStore_WithAValidObject_SetsDataInDataStore()
        {
            // Arrange
            string testKey = "testKey";
            string testValue = "testValue";

            // Act
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            _mockMemoryCache.Setup(x => x.CreateEntry(testKey)).Returns(Mock.Of<ICacheEntry>());
            await _redditDataService.SaveToDataStore(testKey, testValue, TimeSpan.FromMinutes(1));

            // Assert
            _mockDataStore.Verify(x => x.WriteTAsync(testKey, testValue), Times.Once);
        }

        [Fact]
        public async Task GetFromDataStore_WithAValidKeyInCache_RetrievesFromCache()
        {
            // Arrange
            string testKey = "testKey";
            object? testValue = "testValue";

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out testValue)).Returns(true);

            // Act
            var result = await _redditDataService.GetFromDataStore<string?>(testKey);

            // Assert
            Assert.Equal(testValue, result);
        }

        [Fact]
        public async Task GetFromDataStore_WithAValidKeyNotInCache_RetrievesFromDataStore()
        {
            // Arrange
            string testKey = "testKey";
            object? empty = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out empty)).Returns(false);

            // Act
            var result = await _redditDataService.GetFromDataStore<string?>(testKey);

            // Assert
            _mockDataStore.Verify(x => x.GetTAsync<string>(testKey), Times.Once);
        }
    }
}