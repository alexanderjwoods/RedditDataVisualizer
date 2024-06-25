using SDV.Data.Reddit.Interfaces;
using System.Net;

namespace SDV.Data.Reddit.Models
{
    public class RedditApiResponseWrapper<T> : IRedditApiResponseWrapper<T?> where T : class
    {
        public RedditApiResponseWrapper(HttpStatusCode httpStatusCode, T? value = null, string? content = null)
        {
            HttpStatusCode = httpStatusCode;
            Value = value;
            Content = content;
        }

        /// <inheritdoc />
        public HttpStatusCode HttpStatusCode { get; private set; }

        /// <inheritdoc />
        public T? Value { get; private set; }

        /// <inheritdoc/>
        public string? Content { get; private set; }
    }
}