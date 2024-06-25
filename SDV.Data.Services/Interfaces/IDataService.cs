using SDV.Data.Reddit.Models.ApiResponseModels;

namespace SDV.Data.Services.Interfaces
{
    public interface IDataService
    {
        /// <summary>
        /// Authenticate to the Reddit API
        /// </summary>
        /// <returns><see cref="bool"/>Boolean indication of success</returns>
        Task<bool> Authenticate();

        /// <summary>
        /// Get the newest posts from a subreddit in chunks
        /// </summary>
        /// <param name="subreddit">Subreddit name to get posts from</param>
        /// <param name="after">Last post name in the previous chunk.</param>
        /// <returns><see cref="SubRedditPostsResponseDataModel"/></returns>
        Task<SubRedditPostsResponseDataModel> GetNewestPostsBySubredditChunked(string subreddit, string after);

        /// <summary>
        /// Save data to the data store or cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key the object will be saved under</param>
        /// <param name="data">Data to save</param>
        /// <param name="cacheExpiration">Timespan to save value in cache for, or null to not save in cache</param>
        /// <returns><see cref="bool"/>Success Token</returns>
        Task<bool> SaveToDataStore<T>(string key, T data, TimeSpan? cacheExpiration = null);

        /// <summary>
        /// Retrieve data from the data store or cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key the object was saved under</param>
        /// <param name="bypassCache">Bypass the cache and get the object from the data store</param>
        /// <returns>Object as T, or null if no object found</returns>
        Task<T?> GetFromDataStore<T>(string key, bool bypassCache = false);
    }
}