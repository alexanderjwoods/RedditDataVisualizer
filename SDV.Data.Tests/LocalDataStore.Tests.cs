using Microsoft.Extensions.Logging;
using Moq;

namespace SDV.Data.Tests
{
    public class LocalDataStoreTests
    {
        private readonly Mock<ILogger> _mockLogger = new();
        private readonly LocalDataStore _localDataStore;

        public LocalDataStoreTests()
        {
            _localDataStore = new LocalDataStore(_mockLogger.Object);
        }

        [Fact]
        public async Task WriteTAsync_Success()
        {
            // Arrange
            string key = "testKey";
            string value = "testValue";

            // Act
            bool result = await _localDataStore.WriteTAsync(key, value);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WriteTAsync_NullKey_ThrowsArgumentException()
        {
            // Arrange
            string key = null;
            string value = "testValue";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _localDataStore.WriteTAsync(key, value));
        }

        [Fact]
        public async Task WriteTAsync_NullValue_ReturnsFalse()
        {
            // Arrange
            string key = "testKey";
            string value = null;

            // Act
            bool result = await _localDataStore.WriteTAsync(key, value);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetTAsync_Success()
        {
            // Arrange
            string key = "testKey";
            string expectedValue = "testValue";
            await _localDataStore.WriteTAsync(key, expectedValue);

            // Act
            var result = await _localDataStore.GetTAsync<string>(key);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public async Task GetTAsync_NullKey_ThrowsArgumentException()
        {
            // Arrange
            string key = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _localDataStore.GetTAsync<string>(key));
        }

        [Fact]
        public async Task GetTAsync_KeyDoesNotExist_ReturnsNull()
        {
            // Arrange
            string key = "nonExistingKey";

            // Act
            var result = await _localDataStore.GetTAsync<string>(key);

            // Assert
            Assert.Null(result);
        }
    }
}