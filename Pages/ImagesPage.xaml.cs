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
            var images = await ContainerCliService.ListImagesAsync();
            ImagesList.ItemsSource = images;

            if (images.Count == 0)
            {
                StatusText.Text = "No images found.";
                StatusText.Visibility = Visibility.Visible;
            }
            else
            {
                ImagesList.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error listing images: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }
}
