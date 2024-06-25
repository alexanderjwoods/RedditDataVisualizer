using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using SDV.Data.Services.Interfaces;
using SDV.ViewModels;

namespace SDV.UITests.ViewModels
{
    public class IndexViewModelTests
    {
        private Mock<IDataService> _mockDataService = new();
        private readonly Mock<ILogger<IndexViewModel>> _mockLogger = new();
        private readonly Mock<IJSRuntime> _mockJsRuntime = new();
        private readonly IndexViewModel _viewModel;

        public IndexViewModelTests()
        {
            _viewModel = new IndexViewModel(_mockLogger.Object, _mockDataService.Object, _mockJsRuntime.Object);
        }

        [Fact]
        public async Task InitAsync_CallsSaveToDataStoreWithCorrectParameters()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);

            // Act
            await _viewModel.InitAsync();

            // Assert
            _mockDataService.Verify(x => x.SaveToDataStore(It.Is<string>(s => s.StartsWith("InitialPosts_")), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void SubRedditName_Change_TriggersInitAsync()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);
            var initialName = _viewModel.SubRedditName;
            var newName = "newSubreddit";

            // Act
            _viewModel.SubRedditName = newName;

            // Assert
            _mockDataService.Verify(x => x.SaveToDataStore(It.Is<string>(s => s == $"InitialPosts_{newName}"), It.IsAny<HashSet<string>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task ViewMatureContent_Change_FiltersPosts()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);

            // Act
            await _viewModel.InitAsync();
            while (_viewModel.PostsToDisplay?.Count() != 2) { continue; }
            _viewModel.ViewMatureContent = true;
            await Task.Delay(1000);

            // Assert
            Assert.Equal(3, _viewModel.PostsToDisplay?.Count());
        }

        [Fact]
        public void ViewByPosts_TogglesViewCorrectly()
        {
            // Arrange
            _viewModel.IsViewingPostsByUpvote = false; // Ensure initial state

            // Act
            _viewModel.ViewByPosts(new MouseEventArgs());

            // Assert
            Assert.True(_viewModel.IsViewingPostsByUpvote);
            Assert.False(_viewModel.IsViewingPostsByAuthors);
        }

        [Fact]
        public async Task DownloadData_CallsJSInteropWithCorrectParameters()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);
            var jsRuntimeMock = _mockJsRuntime.Setup(x => x.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                It.IsAny<string>(),
                It.IsAny<object[]>())).Returns(It.IsAny<ValueTask<Microsoft.JSInterop.Infrastructure.IJSVoidResult>>);

            await _viewModel.InitAsync();
            while (_viewModel.PostsToDisplay?.Count() != 2) { continue; }

            // Act
            _viewModel.DownloadData(new MouseEventArgs());

            // Assert
            _mockJsRuntime.Verify(x => x.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                "downloadFileFromStream",
                It.Is<object[]>(args => args.Length == 2)),
                Times.Once);
        }
    }
}
