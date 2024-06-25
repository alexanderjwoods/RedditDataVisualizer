using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using SDV.Data.Services.Interfaces;

namespace SDV.UITests.Pages
{
    public class IndexTests : TestContext
    {
        Mock<IDataService> _mockDataService = new();
        Mock<ILogger<Index>> _mockLogger = new();
        Mock<JSRuntime> _mockJsRuntime = new();

        public IndexTests()
        {
            Services.AddSingleton(_mockDataService.Object);
            Services.AddSingleton(_mockLogger.Object);
            Services.AddSingleton(_mockJsRuntime.Object);
        }
        [Fact]
        public void IndexComponent_RendersCorrectly()
        {
            // Act
            var cut = RenderComponent<SDV.Pages.Index>();

            // Assert
            Assert.Contains("Looking at the subreddit r/", cut.Markup);
        }

        [Fact]
        public void SubredditNameInput_UpdatesViewModel()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);
            var cut = RenderComponent<SDV.Pages.Index>();

            // Act
            cut.InvokeAsync(() => cut.Find("#subRedditName").Change("testSubreddit"));

            // Assert
            Assert.Equal("testSubreddit", cut.Instance.ViewModel!.SubRedditName);
        }

        [Fact]
        public void ViewMatureContentCheckbox_UpdatesViewModel()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var checkbox = cut.Find("#over18Filter");

            // Act
            checkbox.Change(true);

            // Assert
            Assert.True(cut.Instance.ViewModel!.ViewMatureContent);
        }

        [Fact]
        public void ViewByPostsButton_Click_ChangesAuthorsCssButtonToOutline()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var button = cut.Find("label[for='newPostsToggleBtn']");

            // Act
            button.Click();

            // Assert
            Assert.Equal("btn btn-outline-success", cut.Instance.ViewModel!.PostsByAuthorsButtonCss);
        }

        [Fact]
        public void ViewByAuthorsButton_Click_ChangesByPostCssButtonToOutline()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var button = cut.Find("label[for='distinctAuthorsToggleBtn']");

            // Act
            button.Click();

            // Assert
            Assert.Equal("btn btn-outline-success", cut.Instance.ViewModel!.ViewByPostsButtonCss);
        }

        [Fact]
        public void ViewByPostsButton_Click_ChangesAuthorsColumnToHidden()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var button = cut.Find("label[for='newPostsToggleBtn']");

            // Act
            button.Click();

            // Assert
            Assert.Equal("col collapse", cut.Instance.ViewModel!.PostsByAuthorsColumnCss);
        }

        [Fact]
        public void ViewByAuthorsButton_Click_ChangesByPostColumnToHidden()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var button = cut.Find("label[for='distinctAuthorsToggleBtn']");

            // Act
            button.Click();

            // Assert
            Assert.Equal("col collapse", cut.Instance.ViewModel!.ViewByPostsColumnCss);
        }

        [Fact]
        public void SortByUpvotes_ClickOnce_SortsDescending()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var sortByUpvotesHeader = cut.Find("#sortableScore");

            // Act
            sortByUpvotesHeader.Click();

            // Assert
            Assert.Contains("clikable oi oi-chevron-top", cut.Instance.ViewModel!.ScoreColumnCss);
        }

        [Fact]
        public void SortByUpvotes_ClickTwice_SortsAscending()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var sortByUpvotesHeader = cut.Find("#sortableScore");

            // Act
            sortByUpvotesHeader.Click();
            sortByUpvotesHeader.Click();

            // Assert
            Assert.Contains("clikable oi oi-chevron-bottom", cut.Instance.ViewModel!.ScoreColumnCss);
        }

        [Fact]
        public void SortByUpvotes_Click_HidesChevronFromComments()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var sortByUpvotesHeader = cut.Find("#sortableScore");

            // Act
            sortByUpvotesHeader.Click();
            sortByUpvotesHeader.Click();

            // Assert
            Assert.Contains("collapse", cut.Instance.ViewModel!.CommentsColumnCss);
        }

        [Fact]
        public void SortByComments_ClickOnce_SortsDescending()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var sortByCommentsHeader = cut.Find("#sortableComments");

            // Act
            sortByCommentsHeader.Click();

            // Assert
            Assert.Contains("clickable oi oi-chevron-bottom", cut.Instance.ViewModel!.CommentsColumnCss);
        }

        [Fact]
        public void SortByComments_ClickTwice_SortsAscending()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var sortByCommentsHeader = cut.Find("#sortableComments");

            // Act
            sortByCommentsHeader.Click();
            sortByCommentsHeader.Click();

            // Assert
            Assert.Contains("clickable oi oi-chevron-top", cut.Instance.ViewModel!.CommentsColumnCss);
        }

        [Fact]
        public void SortByComments_Click_HidesChevronFromScore()
        {
            // Arrange
            var cut = RenderComponent<SDV.Pages.Index>();
            var sortByCommentsHeader = cut.Find("#sortableComments");

            // Act
            sortByCommentsHeader.Click();

            // Assert
            Assert.Contains("collapse", cut.Instance.ViewModel!.ScoreColumnCss);
        }

        [Fact]
        public void PostsTable_RendersCorrectData()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);

            var cut = RenderComponent<SDV.Pages.Index>();
            var linkElement = cut.WaitForElement("#byPostTable > tr:nth-child(1)");

            // Assert
            Assert.Equal("Test Post 1", linkElement.Children[0].Children[0].InnerHtml);
            Assert.Equal("Author1", linkElement.Children[1].InnerHtml);
            Assert.Equal("100", linkElement.Children[2].Children[0].InnerHtml);
            Assert.Equal("10", linkElement.Children[2].Children[1].InnerHtml);
            Assert.Equal("5", linkElement.Children[3].InnerHtml);
            Assert.Contains("Url", linkElement.Children[0].InnerHtml);
        }

        [Fact]
        public void PostsByAuthorsTable_RendersCorrectData()
        {
            // Arrange
            UITestMockHelpers.SetUpStandardDataServiceMocks(ref _mockDataService);

            var cut = RenderComponent<SDV.Pages.Index>();
            var button = cut.Find("label[for='distinctAuthorsToggleBtn']");
            var linkElement = cut.WaitForElement("#byAuthorTable > tr:nth-child(1)");

            // Assert
            Assert.Equal("Author1", linkElement.Children[0].Children[0].InnerHtml);
            Assert.Equal("2", linkElement.Children[1].InnerHtml);
        }

        // Additional tests can be written for sorting functionality, PostsByAuthors button click, and verifying table content.
    }
}
