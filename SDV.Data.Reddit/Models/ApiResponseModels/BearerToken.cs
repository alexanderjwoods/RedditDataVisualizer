using Newtonsoft.Json;

namespace SDV.Data.RedditApi.Models.ApiResponseModels
{
    public class BearerToken
    {
        /// <summary>
        /// Token used to authenticate requests to the Reddit API
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// How many seconds the token is valid for
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// The scope of access to the Reddit API the token has
        /// </summary>
        [JsonProperty("scope")]
        public string Scope { get; set; }

        /// <summary>
        /// DateTime of token Expiry
        /// </summary>
        public DateTime Expiry { get; private set; }

        public BearerToken(string access_token, string token_type, int expiresIn, string scope)
        {
            AccessToken = access_token ?? throw new ArgumentNullException(nameof(access_token));
            TokenType = token_type ?? "bearer";
            ExpiresIn = expiresIn;
            Scope = scope ?? "*";
            Expiry = DateTime.Now.AddSeconds(ExpiresIn);
        }
    }
}