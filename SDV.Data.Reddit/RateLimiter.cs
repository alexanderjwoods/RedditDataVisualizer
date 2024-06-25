namespace SDV.Data.RedditApi
{
    public class RateLimiter
    {
        private double _rateLimitRemaining = int.MaxValue;
        private DateTime _rateLimitResetTime = DateTime.UtcNow;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly int _maxWaitSeconds = 2;

        public async Task UpdateRateLimitStateAsync(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remainingValues) &&
                response.Headers.TryGetValues("x-ratelimit-reset", out var resetValues))
            {
                var remaining = remainingValues.FirstOrDefault();
                var reset = resetValues.FirstOrDefault();

                if (double.TryParse(remaining, out double remainingRequests) && double.TryParse(reset, out double resetInSeconds))
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        _rateLimitRemaining = remainingRequests;
                        _rateLimitResetTime = DateTime.UtcNow.AddSeconds(resetInSeconds);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
        }

        public async Task EnsureRateLimitAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_rateLimitRemaining <= 1)
                {
                    var delayDuration = _rateLimitResetTime - DateTime.UtcNow;
                    if (delayDuration > TimeSpan.Zero)
                    {
                        await Task.Delay(delayDuration);
                    }
                }
                else
                {
                    var timeUntilReset = (_rateLimitResetTime - DateTime.UtcNow).TotalSeconds;
                    var delayDuration = TimeSpan.FromSeconds(timeUntilReset / _rateLimitRemaining);

                    if (_rateLimitRemaining > 50)
                    {
                        delayDuration = TimeSpan.FromSeconds(Math.Min(delayDuration.TotalSeconds, _maxWaitSeconds));
                    }

                    if (delayDuration > TimeSpan.Zero)
                    {
                        await Task.Delay(delayDuration);
                    }

                    _rateLimitRemaining--;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}