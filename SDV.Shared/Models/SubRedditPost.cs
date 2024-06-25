using Newtonsoft.Json;
using SDV.Shared.Interfaces;

namespace SDV.Shared.Models
{
    public class SubRedditPost : ISubRedditPost
    {
        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "title")]
        public string Title { get; set; } = string.Empty;

        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "author")]
        public string Author { get; set; } = string.Empty;

        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "num_comments")]
        public int NumComments { get; set; }

        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "url")]
        public string Url { get; set; } = string.Empty;

        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "subreddit_name_prefixed")]
        public string SubredditName { get; set; } = string.Empty;

        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "ups")]
        public int Upvotes { get; set; }

        ///<inheritdoc />
        [JsonProperty(Required = Required.Always, PropertyName = "downs")]
        public int Downvotes { get; set; }

        ///<inheritdoc/>
        [JsonProperty(Required = Required.Always, PropertyName = "over_18")]
        public bool MatureContent { get; set; }

        ///<inheritdoc />
        [JsonIgnore]
        public DateTime SynchronizedLast { get; set; } = DateTime.Now;
    }
}