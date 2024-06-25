using SDV.Shared.Interfaces;

namespace SDV.Models
{
    public class IndexModel
    {
        public List<Post> Posts { get; set; } = new List<Post>();

        public static IndexModel Create(IEnumerable<ISubRedditPost> posts)
        {
            var list = posts.Select(p => new Post()
            {
                Title = p.Title,
                Author = p.Author,
                Upvotes = p.Upvotes,
                Downvotes = p.Downvotes,
                NumComments = p.NumComments,
                SynchedOn = p.SynchronizedLast.ToShortTimeString(),
                PostUrl = p.Url
            });

            return new IndexModel()
            {
                Posts = list.ToList()
            };
        }
    }

    public class Post
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Upvotes { get; set; } = 0;
        public int Downvotes { get; set; } = 0;
        public int NumComments { get; set; } = 0;
        public string SynchedOn { get; set; } = string.Empty;
        public string PostUrl { get; set; } = string.Empty;
    }
}