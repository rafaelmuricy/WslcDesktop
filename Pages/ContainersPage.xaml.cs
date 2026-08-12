using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WslcDesktop.Models;
using WslcDesktop.Services;

namespace WslcDesktop.Pages;

public sealed partial class ContainersPage : Page
{
    private bool _loaded;

    public ContainersPage()
    {
        InitializeComponent();
    }

    private async void ContainersPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadContainersAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadContainersAsync();
    }

    private async void StartStopContainerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string containerId } || string.IsNullOrWhiteSpace(containerId))
        {
            return;
        }

        var isRunning = sender is FrameworkElement { DataContext: ContainerInstance container } && container.IsRunning;

        try
        {
            if (isRunning)
            {
                await ContainerCliService.StopContainerAsync(containerId);
            }
            else
            {
                await ContainerCliService.StartContainerAsync(containerId);
            }

            await LoadContainersAsync();
        }
        catch (Exception ex)
        {
            var action = isRunning ? "stopping" : "starting";
            StatusText.Text = $"Error {action} container: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
    }

    private async void DeleteContainerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string containerId } || string.IsNullOrWhiteSpace(containerId))
        {
            return;
        }

        try
        {
            await ContainerCliService.DeleteContainerAsync(containerId);
            await LoadContainersAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error deleting container: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadContainersAsync()
    {
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        ContainersList.Visibility = Visibility.Collapsed;
        StatusText.Visibility = Visibility.Collapsed;

        try
        {
            var containers = await ContainerCliService.ListContainersAsync();
            ContainersList.ItemsSource = containers;

            if (containers.Count == 0)
            {
                StatusText.Text = "No containers found.";
                StatusText.Visibility = Visibility.Visible;
            }
            else
            {
                ContainersList.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error listing containers: {ex.Message}";
            StatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }
}
