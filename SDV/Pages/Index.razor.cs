using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SDV.Data.Services.Interfaces;
using SDV.Models;
using SDV.ViewModels;
using System.ComponentModel;
using System.Text;

namespace SDV.Pages
{
    public partial class Index : ComponentBase, INotifyPropertyChanged
    {
        [Inject] public IndexViewModel? ViewModel { get; set; }
        [Inject] private IJSRuntime? _js { get; set; }
        [Inject] private IDataService? _dataService { get; set; }
        [Inject] private ILogger<Index>? _logger { get; set; }
        [Inject] private IConfiguration _configuration { get; set; }

        private CancellationTokenSource? pollingCancellationTokenSource;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private IndexModel model = new();
        [Parameter]
        public IndexModel Model
        {
            get => model;
            set
            {
                model = value;
                StateHasChanged();
            }
        }

        private string subredditName;
        [Parameter]
        public string SubredditName
        {
            get => subredditName;
            set
            {
                if (subredditName != value)
                {
                    subredditName = value;
                    OnPropertyChanged(nameof(SubredditName));
                }
            }
        }

        public string? errorMessage;
        [Parameter]
        public string? ErrorMessage
        {
            get => errorMessage;
            set
            {
                if (errorMessage != value)
                {
                    errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        [Parameter]
        public string PollingStartedAt { get; set; } = DateTime.Now.ToShortTimeString();
        public Dictionary<string, int> CalculateAuthorPosts => CalculatePostsByAuthor();

        public string SortedBy { get; set; } = "Upvotes";

        public List<Post> SortedPosts => model.Posts.OrderByDescending(
            x => SortedBy is "Downvotes" ? x.Downvotes
            : SortedBy is "Comments" ? x.NumComments 
            : x.Upvotes).ToList();

        public Dictionary<string, int> CalculatePostsByAuthor()
        {
            if (Model.Posts is null || !Model.Posts.Any())
            {
                return new();
            }

            return Model.Posts.GroupBy(x => x.Author)
                        .ToDictionary(g => g.First().Author, g => g.Count())
                        .OrderByDescending(x => x.Value)
                        .ToDictionary(x => x.Key, x => x.Value);
        }

        private bool isViewingPostsByUpvote = true;

        public bool IsViewingPostsByUpvote
        {
            get => isViewingPostsByUpvote;
            set
            {
                if (value != isViewingPostsByUpvote)
                {
                    isViewingPostsByUpvote = value;
                    OnPropertyChanged(nameof(IsViewingPostsByUpvote));
                }
            }
        }

        protected override void OnInitialized()
        {
            SubredditName = _configuration["AppSettings:SubredditName"]!;
            if (ViewModel is not null)
            {
                try
                {
                    pollingCancellationTokenSource = new CancellationTokenSource();
                    ViewModel.InitPolling(SubredditName, pollingCancellationTokenSource.Token);
                    ViewModel.IndexModelUpdated += IndexModelHasUpdated;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing polling");
                    SetError("An error occured initializing the calls to the database.  Please reload.");
                }
            }
        }

        private void IndexModelHasUpdated(IndexModel model)
        {
            try
            {
                InvokeAsync(() =>
                {
                    Model = model;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating model");
                SetError("An error occured getting the new data");
            }
        }

        public async void DownloadData(MouseEventArgs e)
        {
            try
            {
                var fileStream = GenerateCSV();
                var fileName = $"RedditPosts_{SubredditName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";

                using var streamRef = new DotNetStreamReference(fileStream);

                await _js.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV");
                SetError("Error generating CSV");
            }

            Stream GenerateCSV()
            {
                StringBuilder sb = new();
                sb.AppendLine("Top Posts");
                sb.AppendLine("Title,Author,Upvotes,Downvotes,Comments,Url");
                foreach (var post in Model.Posts)
                {
                    sb.AppendLine($"{post.Title}, {post.Author}, {post.Upvotes},{post.Downvotes},{post.NumComments},{post.PostUrl}");
                }
                sb.AppendLine();
                sb.AppendLine("Distinct Authors");
                sb.AppendLine("Author,PostCount");
                foreach (var author in CalculatePostsByAuthor())
                {
                    sb.AppendLine($"{author.Key},{author.Value}");
                }

                return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            }
        }

        public void SetError(string error)
        {
            ErrorMessage = error;
            if (ErrorMessage is not null)
            {
                var timer = new Timer(o =>
                {
                    ErrorMessage = string.Empty;
                }, null, 10000, Timeout.Infinite);
            }
        }
    }
}