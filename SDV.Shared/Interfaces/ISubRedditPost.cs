namespace SDV.Shared.Interfaces
{
    public interface ISubRedditPost
    {
        /// <summary>
        /// Title of the post
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Username of the author
        /// </summary>
        string Author { get; set; }

        /// <summary>
        /// Number of comments on the post
        /// </summary>
        int NumComments { get; set; }

        /// <summary>
        /// Url of the post
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// Count of "upvotes" for the post
        /// </summary>
        int Upvotes { get; set; }

        /// <summary>
        /// Count of "downvotes" for the post
        /// </summary>
        int Downvotes { get; set; }

        /// <summary>
        /// Has the post been marked as Over 18
        /// </summary>
        bool MatureContent { get; set; }

        /// <summary>
        /// The date and time the post record was last synched
        /// </summary>
        DateTime SynchronizedLast { get; set; }
    }
}