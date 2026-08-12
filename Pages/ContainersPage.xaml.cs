using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WslcDesktop.Models;
using WslcDesktop.Services;

namespace WslcDesktop.Pages;

public sealed partial class ContainersPage : Page
{
    private bool _loaded;
    private IReadOnlyList<ContainerInstance> _allContainers = [];

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

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ApplyFilters();
        }
    }

    private void OnlyRunningToggle_Toggled(object sender, RoutedEventArgs e) => ApplyFilters();

    private void ContainersList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ContainerInstance container)
        {
            return;
        }

        Frame.Navigate(typeof(ContainerDetailsPage), container);
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
            _allContainers = await ContainerCliService.ListContainersAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _allContainers = [];
            ContainersList.ItemsSource = null;
            StatusText.Text = $"Error listing containers: {ex.Message}";
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
        var onlyRunning = OnlyRunningToggle.IsOn;

        IEnumerable<ContainerInstance> filtered = _allContainers;

        if (onlyRunning)
        {
            filtered = filtered.Where(container => container.IsRunning);
        }

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(container => MatchesSearch(container, query));
        }

        var result = filtered.ToList();
        ContainersList.ItemsSource = result;

        if (_allContainers.Count == 0)
        {
            StatusText.Text = "No containers found.";
            StatusText.Visibility = Visibility.Visible;
            ContainersList.Visibility = Visibility.Collapsed;
            return;
        }

        if (result.Count == 0)
        {
            StatusText.Text = "No containers match the current filters.";
            StatusText.Visibility = Visibility.Visible;
            ContainersList.Visibility = Visibility.Collapsed;
            return;
        }

        StatusText.Visibility = Visibility.Collapsed;
        ContainersList.Visibility = Visibility.Visible;
    }

    private static bool MatchesSearch(ContainerInstance container, string query)
    {
        return Contains(container.Id, query)
            || Contains(container.ShortId, query)
            || Contains(container.Name, query)
            || Contains(container.Image, query)
            || Contains(container.StatusDisplay, query)
            || Contains(container.PortsDisplay, query);
    }

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
