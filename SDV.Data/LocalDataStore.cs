using Microsoft.Extensions.Logging;
using SDV.Data.Interfaces;
using System.Collections.Concurrent;

namespace SDV.Data
{
    public class LocalDataStore : IDataStore
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, object> _dataStore = new();

        public LocalDataStore(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<T?> GetTAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogError("Key cannot be null or whitespace");
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            _dataStore.TryGetValue(key, out var value);

            return Task.FromResult((T?)value);
        }

        /// <inheritdoc />
        public Task<bool> WriteTAsync<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogError("Key cannot be null or whitespace");
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            if (value == null)
            {
                return Task.FromResult(false);
            }

            bool result = _dataStore.AddOrUpdate(key, value, (k, v) => value) != null;

            return Task.FromResult(result);
        }
    }
}