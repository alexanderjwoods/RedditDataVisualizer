using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using SDV.Data.Reddit;
using System.Net;

namespace SDV.Data.RedditApi.Tests
{
    public class RedditApiClientTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly Mock<IConfiguration> _mockConfiguration = new();
        private readonly RedditApiClient _redditApiClient;

        public RedditApiClientTests()
        {
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns("TestValue");
            _redditApiClient = new RedditApiClient(_httpClientFactoryMock.Object, new NullLogger<RedditApiClient>(), _mockConfiguration.Object);
        }

        [Fact]
        public async Task Authenticate_ReturnsToken_WhenCalledWithValidCredentials()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\": \"test_token\", \"token_type\": \"bearer\"}")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            var result = await _redditApiClient.Authenticate("clientId", "clientSecret");

            // Assert
            Assert.Equal("test_token", result.Value?.AccessToken);
        }

        [Fact]
        public void Authenticate_ThrowsException_WhenCalledWithEmptyClientId()
        {
            // Arrange
            var clientSecret = "clientSecret";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _redditApiClient.Authenticate("", clientSecret));
            Assert.Equal("'clientId' cannot be null or whitespace. (Parameter 'clientId')", ex?.Result.Message);
        }

        [Fact]
        public void Authenticate_ThrowsException_WhenCalledWithEmptyClientSecret()
        {
            // Arrange
            var clientId = "clientId";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _redditApiClient.Authenticate(clientId, ""));
            Assert.Equal("'clientSecret' cannot be null or whitespace. (Parameter 'clientSecret')", ex?.Result.Message);
        }

        [Fact]
        public async Task GetNewPostsAsync_ReturnsPosts_WhenCalledWithValidToken()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"data\": {\"children\": []}}")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            var result = await _redditApiClient.GetNewPostsAsync("bearerToken", "subreddit");

            // Assert
            Assert.Empty(result.Value!.Data.Children);
        }

        [Fact]
        public void GetNewPostsAsync_ThrowsException_WhenCalledWithEmptyBearerToken()
        {
            // Arrange
            var subreddit = "subreddit";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _redditApiClient.GetNewPostsAsync("", subreddit));
            Assert.Equal("'bearerToken' cannot be null or whitespace. (Parameter 'bearerToken')", ex?.Result.Message);
        }

        [Fact]
        public void GetNewPostsAsync_ThrowsException_WhenCalledWithEmptySubreddit()
        {
            // Arrange
            var bearerToken = "bearerToken";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _redditApiClient.GetNewPostsAsync(bearerToken, ""));
            Assert.Equal("'subreddit' cannot be null or whitespace. (Parameter 'subreddit')", ex?.Result.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "400 (Bad Request)")]
        [InlineData(HttpStatusCode.Unauthorized, "401 (Unauthorized)")]
        [InlineData(HttpStatusCode.InternalServerError, "500 (Internal Server Error)")]
        [InlineData(HttpStatusCode.TooManyRequests, "429 (Too Many Requests)")]
        public void Authenticate_ThrowsHttpException_WhenApiResponseIsUnauthorized(HttpStatusCode httpStatusCode, string expected)
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent("")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await _redditApiClient.Authenticate("invalidClientId", "invalidClientSecret"));
            Assert.Equal(expected, ex?.Result.Message);
        }

        [Fact]
        public async Task GetNewPostsAsync_WaitsForRateLimitReset_WhenRateLimitIsExceeded()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    var response = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"data\": {\"children\": []}}")
                    };
                    response.Headers.Add("x-ratelimit-remaining", "0");
                    response.Headers.Add("x-ratelimit-reset", "2");
                    return response;
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var client2 = new HttpClient(mockHttpMessageHandler.Object);

            _httpClientFactoryMock.SetupSequence(x => x.CreateClient(It.IsAny<string>()))
                .Returns(client)
                .Returns(client2);

            // Act
            await _redditApiClient.GetNewPostsAsync("bearerToken", "subreddit"); // First request to set the rate limit
            var startTime = DateTime.UtcNow;
            await _redditApiClient.GetNewPostsAsync("bearerToken", "subreddit"); // Now we check it
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.True((endTime - startTime).TotalSeconds > 1, "The method did not wait for the rate limit reset.");
        }

        [Fact]
        public async Task GetNewPostsAsync_ThrowsJsonException_WhenApiResponseHasUnexpectedStructure()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ \"kind\": \"Listing\" }")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<JsonSerializationException>(async () => await _redditApiClient.GetNewPostsAsync("bearerToken", "subreddit"));
        }
    }
}