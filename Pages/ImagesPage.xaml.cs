using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using WslcDesktop.Dialogs;
using WslcDesktop.Models;
using WslcDesktop.Services;

namespace WslcDesktop.Pages;

public sealed partial class ImagesPage : Page
{
    private bool _loaded;
    private IReadOnlyList<ContainerImage> _allImages = [];

    public ImagesPage()
    {
        InitializeComponent();
    }

    private async void ImagesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadImagesAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadImagesAsync();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ApplyFilters();
        }
    }

    private async void AddImageButton_Click(object sender, RoutedEventArgs e)
    {

        var filePicker = new FileOpenPicker(App.Window.AppWindow.Id);


        var result = await filePicker.PickSingleFileAsync();

        if (result == null)
            return;

        var fileName = new FileInfo(result.Path).Name;

        if (fileName != "Dockerfile" && fileName != "Containerfile")
        {
            StatusText.Text = "Select a Dockerfile or Containerfile.";
            StatusText.Visibility = Visibility.Visible;
            return;
        }


        var imageName = Path.GetDirectoryName(result.Path)!.Split("\\").Last().Replace(" ", "");
        
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        //ImagesList.Visibility = Visibility.Collapsed;
        StatusText.Text = $"Executing container build -f \"{result.Path}\" -t {imageName}...";
        StatusText.Visibility = Visibility.Visible;

        try
        {
            await ContainerCliService.BuildAsync(result.Path, imageName);
            await LoadImagesAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error building image: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private async void RunImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string imageReference } || string.IsNullOrWhiteSpace(imageReference))
        {
            return;
        }

        var imageId = sender is FrameworkElement { DataContext: ContainerImage image }
            ? image.ShortId
            : imageReference;

        var dialog = new RunContainerDialog(imageReference, imageId);
        var confirmed = await dialog.ShowAsync();
        if (!confirmed)
        {
            return;
        }

        try
        {
            await ContainerCliService.RunContainerAsync(
                dialog.ImageReference,
                dialog.ContainerName,
                dialog.GetPortMappings(),
                dialog.GetVolumeMappings(),
                dialog.GetEnvironmentVariables());

            StatusText.Text = $"Container started from {imageReference}.";
            StatusText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error starting container: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
    }

    private async void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string imageId } || string.IsNullOrWhiteSpace(imageId))
        {
            return;
        }

        try
        {
            await ContainerCliService.DeleteImageAsync(imageId);
            await LoadImagesAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error deleting image: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
    }


    private async Task LoadImagesAsync()
    {
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        ImagesList.Visibility = Visibility.Collapsed;
        StatusText.Visibility = Visibility.Collapsed;

        try
        {
            _allImages = await ContainerCliService.ListImagesAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _allImages = [];
            ImagesList.ItemsSource = null;
            StatusText.Text = $"Error listing images: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyFilters()
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;

        IEnumerable<ContainerImage> filtered = _allImages;

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(image => MatchesSearch(image, query));
        }

        var result = filtered.ToList();
        ImagesList.ItemsSource = result;

        if (_allImages.Count == 0)
        {
            StatusText.Text = "No images found.";
            StatusText.Visibility = Visibility.Visible;
            ImagesList.Visibility = Visibility.Collapsed;
            return;
        }

        if (result.Count == 0)
        {
            StatusText.Text = "No images match the current filters.";
            StatusText.Visibility = Visibility.Visible;
            ImagesList.Visibility = Visibility.Collapsed;
            return;
        }

        StatusText.Visibility = Visibility.Collapsed;
        ImagesList.Visibility = Visibility.Visible;
    }

    private static bool MatchesSearch(ContainerImage image, string query)
    {
        return Contains(image.Name, query)
            || Contains(image.Tag, query)
            || Contains(image.Id, query)
            || Contains(image.ShortId, query)
            || Contains(image.FullName, query)
            || Contains(image.SizeDisplay, query);
    }

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
