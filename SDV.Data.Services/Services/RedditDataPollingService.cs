using SDV.Data.Services.Interfaces;
using SDV.Shared.Interfaces;

namespace SDV.Data.Services.Services
{
    /// <inheritdoc />
    public class RedditDataPollingService : IRedditDataPollingService
    {
        private IDataService _dataService;

        HashSet<string> initialTitles = new HashSet<string>();
        private string _subredditToPoll;

        /// <inheritdoc />
        public event EventHandler<DataPublishedEventArgs>? DataPublished;

        public RedditDataPollingService(IDataService dataService, string subredditToPoll)
        {
            if (string.IsNullOrEmpty(subredditToPoll))
            {
                throw new ArgumentException($"'{nameof(subredditToPoll)}' cannot be null or empty.", nameof(subredditToPoll));
            }

            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _subredditToPoll = subredditToPoll;
        }

        /// <inheritdoc />
        public void PublishData(IEnumerable<ISubRedditPost> data)
        {
            DataPublished?.Invoke(this, new DataPublishedEventArgs { Data = data });
        }

        /// <inheritdoc />
        public void StartPolling(TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DateTime nextRun = DateTime.UtcNow.Add(interval);

                    var pollTask = PollForData(cancellationToken);

                    while (!pollTask.IsCompleted)
                    {
                        Task.WhenAny(pollTask).Wait(cancellationToken);
                    }

                    if (pollTask.IsCompleted)
                    {
                        PublishData(pollTask.Result);
                    }

                    if (nextRun > DateTime.UtcNow)
                    {
                        Task.Delay(100).Wait(cancellationToken);
                    }
                } while (!cancellationToken.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private async Task<IEnumerable<ISubRedditPost>> PollForData(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (initialTitles is null || initialTitles.Count == 0)
                {
                    initialTitles = await _dataService.GetFromDataStore<HashSet<string>>($"InitialTitles_{_subredditToPoll}") ?? new();

                    if (initialTitles is null || initialTitles.Count == 0)
                    {
                        var tempList = await GetCompletePostList();
                        initialTitles = new HashSet<string>(tempList.Select(x => x.Title));
                        await _dataService.SaveToDataStore($"InitialTitles_{_subredditToPoll}", initialTitles, TimeSpan.FromHours(2));
                    }
                }

                string after = string.Empty;

                var incomingPosts = await GetCompletePostList();

                return incomingPosts.Where(incoming => !initialTitles.Contains(incoming.Title));
            }
            catch (OperationCanceledException)
            {
                return Enumerable.Empty<ISubRedditPost>();
            }

            async Task<IEnumerable<ISubRedditPost>> GetCompletePostList()
            {
                var posts = new List<ISubRedditPost>();
                string after = string.Empty;

                do
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var chunk = await _dataService.GetNewestPostsBySubredditChunked(_subredditToPoll, after);

                        posts.AddRange(chunk.Children.Select(x => x.Post).ToList());

                        after = chunk.After;
                    }
                    else
                    {
                        return Enumerable.Empty<ISubRedditPost>();
                    }
                } while (!string.IsNullOrWhiteSpace(after));

                return posts;
            }
        }
    }

    public class DataPublishedEventArgs : EventArgs
    {
        public IEnumerable<ISubRedditPost> Data { get; set; }
    }
}
