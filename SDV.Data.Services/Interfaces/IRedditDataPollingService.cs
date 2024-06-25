using SDV.Data.Services.Services;
using SDV.Shared.Interfaces;

namespace SDV.Data.Services.Interfaces
{
    /// <summary>
    /// Service for polling Reddit data and publishing new posts to subscribers.
    /// </summary>
    public interface IRedditDataPollingService
    {
        /// <summary>
        /// Subscribe to the DataPublished event to receive updates when new data is published.
        /// </summary>
        event EventHandler<DataPublishedEventArgs>? DataPublished;

        /// <summary>
        /// This method will publish the data to all subscribers of the DataPublished event.
        /// </summary>
        /// <param name="data">Data to send as the message</param>
        void PublishData(IEnumerable<ISubRedditPost> data);

        /// <summary>
        /// Start a polling loop that will poll for data at the specified interval until the cancellation token is cancelled.
        /// </summary>
        /// <param name="interval"><see cref="TimeSpan"/> to wait between API calls at a minimum</param>
        /// <param name="cancellationToken">Cancellation token used to terminate polling</param>
        void StartPolling(TimeSpan interval, CancellationToken cancellationToken);
    }
}
