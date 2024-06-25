using SDV.Data.Reddit.Models;
using SDV.Data.Reddit.Models.ApiResponseModels;
using SDV.Data.RedditApi.Models.ApiResponseModels;

namespace SDV.Data.Reddit.Interfaces
{
    public interface IRedditApiClient
    {
        /// <summary>
        /// Authenticate to the Reddit API
        /// </summary>
        /// param name="clientId">Client ID</param>
        /// param name="clientSecret">Client Secret</param>
        /// <returns><see cref="RedditBearerToken"/></returns>
        Task<RedditApiResponseWrapper<BearerToken>> Authenticate(string clientId, string clientSecret);

        /// <summary>
        /// Retrieves new posts from a subreddit
        /// </summary>
        /// <param name="bearerToken">Bearer token</param>
        /// <param name="subreddit">Subreddit to poll</param>
        /// <param name="after">The name of the previous post retrieved, or blank to retrieve from the top</param>
        /// <returns><see cref="RedditApiResponseWrapper{SubredditPostsResponse}"/></returns>
        Task<RedditApiResponseWrapper<SubRedditPostsResponse>> GetNewPostsAsync(string bearerToken, string subreddit, string after = "");
    }
}