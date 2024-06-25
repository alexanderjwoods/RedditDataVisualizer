using SDV.Data.Services.Interfaces;
using SDV.Data.Services.Services;
using SDV.Models;
using System.ComponentModel;

namespace SDV.ViewModels
{
    public class IndexViewModel
    {
        public Action<IndexModel>? IndexModelUpdated;

        private readonly IDataService _dataService;
        private IRedditDataPollingService? redditDataPollingService;

        public IndexViewModel(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public void InitPolling(string subredditName, CancellationToken cancellationToken)
        {
            if (redditDataPollingService == null)
            {
                redditDataPollingService = new RedditDataPollingService(_dataService, subredditName);
                redditDataPollingService.DataPublished += RedditDataPollingService_DataPublished;

                _ = Task.Run(() => redditDataPollingService.StartPolling(TimeSpan.FromSeconds(2), cancellationToken));
            }
        }

        private void RedditDataPollingService_DataPublished(object? sender, DataPublishedEventArgs e)
        {
            if (e.Data?.Count() > 0)
            {
                IndexModelUpdated?.Invoke(IndexModel.Create(e.Data));
            }
        }
    }
}