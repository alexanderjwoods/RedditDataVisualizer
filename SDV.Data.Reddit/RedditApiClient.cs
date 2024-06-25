using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SDV.Data.Reddit.Interfaces;
using SDV.Data.Reddit.Models;
using SDV.Data.Reddit.Models.ApiResponseModels;
using SDV.Data.RedditApi;
using SDV.Data.RedditApi.Models.ApiResponseModels;
using System.Net.Http.Headers;
using System.Text;

namespace SDV.Data.Reddit
{
    public class RedditApiClient : IRedditApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly RateLimiter _rateLimiter = new();

        public RedditApiClient(IHttpClientFactory httpClientFactory, ILogger logger, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(httpClientFactory);

            _httpClientFactory = httpClientFactory;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public async Task<RedditApiResponseWrapper<BearerToken>> Authenticate(string clientId, string clientSecret)
        {
            ValidateNonEmpty(clientId, nameof(clientId));
            ValidateNonEmpty(clientSecret, nameof(clientSecret));

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));

                HttpRequestMessage requestMessage = new(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token?grant_type=client_credentials");
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                requestMessage.Headers.Add("User-Agent", _configuration["RedditClientSettings:user_agent"]);

                return await SendRequest<BearerToken>(requestMessage, "Failed to authenticate.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the authentication request");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<RedditApiResponseWrapper<SubRedditPostsResponse>> GetNewPostsAsync(string bearerToken, string subreddit, string after = "")
        {
            ValidateNonEmpty(bearerToken, nameof(bearerToken));
            ValidateNonEmpty(subreddit, nameof(subreddit));

            HttpRequestMessage requestMessage = GenerateDefaultGetRequest(bearerToken, @$"https://oauth.reddit.com/r/{subreddit}/new?after={after}&limit=100");

            return await SendRequest<SubRedditPostsResponse>(requestMessage, "An error occurred while sending the request to Reddit API.");
        }

        private async Task<RedditApiResponseWrapper<T>> SendRequest<T>(HttpRequestMessage requestMessage, string errorMessage) where T : class
        {
            using var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response;
            try
            {
                await _rateLimiter.EnsureRateLimitAsync();
                response = await client.SendAsync(requestMessage);
                await _rateLimiter.UpdateRateLimitStateAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage);
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                HandleUnsuccessfulResponse(response);
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(content, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

            return result == null
                ? throw new InvalidOperationException($"Failed to deserialize the response into {typeof(T).Name}.")
                : new RedditApiResponseWrapper<T>(response.StatusCode, result);
        }

        private void ValidateNonEmpty(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"'{paramName}' cannot be null or whitespace.", paramName);
            }
        }

        private void HandleUnsuccessfulResponse(HttpResponseMessage response)
        {
            throw new HttpRequestException($"{(int)response.StatusCode} ({response.ReasonPhrase})");
        }

        private HttpRequestMessage GenerateDefaultGetRequest(string bearerToken, string url)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            requestMessage.Headers.Add("User-Agent", _configuration["RedditClientSettings:user_agent"]);

            return requestMessage;
        }
    }
}