using System.Net;

namespace SDV.Data.Reddit.Interfaces
{
    public interface IRedditApiResponseWrapper<T>
    {
        /// <summary>
        /// HTTP Status Code of the response
        /// </summary>
        HttpStatusCode HttpStatusCode { get; }

        /// <summary>
        /// Object returned from Reddit API
        /// </summary>
        T? Value { get; }

        /// <summary>
        /// The content of the response if not successfully deserialized
        /// </summary>
        string? Content { get; }
    }
}