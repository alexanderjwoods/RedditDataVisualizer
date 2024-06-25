using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SDV.Data.Interfaces;
using SDV.Data.Reddit.Interfaces;
using SDV.Data.Reddit.Models.ApiResponseModels;
using SDV.Data.RedditApi.Models.ApiResponseModels;
using SDV.Data.Services.Interfaces;

namespace SDV.Data.Services.Services
{
    public class RedditDataService : IDataService
    {
        private readonly IDataStore _dataStore;
        private readonly IRedditApiClient _redditApiClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        private static BearerToken? _bearerToken = null;

        public RedditDataService(IConfiguration configuration, IDataStore dataStore, IRedditApiClient redditApiClient, IMemoryCache memoryCache)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _redditApiClient = redditApiClient ?? throw new ArgumentNullException(nameof(redditApiClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <inheritdoc/>
        public async Task<bool> Authenticate()
        {
            var clientId = _configuration["Secrets:client_id"] ?? throw new ArgumentNullException("client_id", "Must specify a clientId to Authenticate to the OAuth2 Reddit API Endpoint");
            var clientSecret = _configuration["Secrets:client_secret"] ?? throw new ArgumentNullException("client_secret", "Must specify a clientSecret to Authenticate to the OAuth2 Reddit API Endpoint");

            var response = await _redditApiClient.Authenticate(clientId, clientSecret);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Failed to authenticate to the Reddit API. HTTP Status Code: {response.HttpStatusCode}");
            }

            _bearerToken = response.Value ?? throw new InvalidOperationException("Authentication response did not include a response object.");

            if (string.IsNullOrWhiteSpace(response.Value.AccessToken))
            {
                throw new InvalidOperationException("Authentication response did not include a valid access token");
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<SubRedditPostsResponseDataModel> GetNewestPostsBySubredditChunked(string subreddit, string after)
        {
            if (_bearerToken is null || _bearerToken.Expiry <= DateTime.Now)
            {
                await Authenticate();
            }

            var response = await _redditApiClient.GetNewPostsAsync(_bearerToken!.AccessToken, subreddit, after);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to retrieve posts from Reddit API. HTTP Status Code: {response.HttpStatusCode}");
            }

            return response.Value?.Data ?? throw new InvalidOperationException("Failed to retrieve posts from Reddit API. Response value is null.");
        }

        /// <inheritdoc/>
        public async Task<bool> SaveToDataStore<T>(string key, T data, TimeSpan? cacheExpiration = null)
        {
            if (cacheExpiration.HasValue)
            {
                _memoryCache.Set(key, data, cacheExpiration.Value);
            }

            return await _dataStore.WriteTAsync(key, data);
        }

        /// <inheritdoc/>
        public async Task<T?> GetFromDataStore<T>(string key, bool bypassCache = false)
        {
            if (!bypassCache && _memoryCache.TryGetValue(key, out T? cachedValue))
            {
                return cachedValue;
            }

            return await _dataStore.GetTAsync<T?>(key);
        }
    }
}