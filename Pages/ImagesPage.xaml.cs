using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
                StatusText.Text = "Nenhuma imagem encontrada.";
                StatusText.Visibility = Visibility.Visible;
            }
            else
            {
                ImagesList.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro ao listar imagens: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }
}
