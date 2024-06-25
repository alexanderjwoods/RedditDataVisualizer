using Newtonsoft.Json;
using SDV.Shared.Models;

namespace SDV.Data.Reddit.Models.ApiResponseModels
{
    public class SubRedditPostsResponse
    {
        /// <summary>
        /// Wrapper for Response data
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "data")]
        public SubRedditPostsResponseDataModel Data { get; set; }
    }

    /// <summary>
    /// Meta Data for the response and the posts under children
    /// </summary>
    public class SubRedditPostsResponseDataModel
    {
        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "children")]
        public List<SubRedditPostsResponseChildModel> Children { get; set; }

        [JsonProperty("before")]
        public object Before { get; set; }
    }

    /// <summary>
    /// Post Children
    /// </summary>
    public class SubRedditPostsResponseChildModel
    {
        [JsonProperty(Required = Required.Always, PropertyName = "data")]
        public SubRedditPost Post { get; set; }
    }
}