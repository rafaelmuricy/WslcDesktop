using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using WslcDesktop.Models;
using WslcDesktop.Services;

namespace WslcDesktop.Pages;

public sealed partial class ContainerDetailsPage : Page
{
    private ContainerInstance? _container;
    private string _activeTab = "Logs";
    private string _fullLogs = string.Empty;
    private bool _autoScroll = true;
    private bool _loaded;

    public ContainerDetailsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ContainerInstance container)
        {
            _container = container;
            BindHeader();
            UpdateActionButtons();
        }
    }

    private async void ContainerDetailsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded || _container is null)
        {
            return;
        }

        _loaded = true;
        SelectTab("Logs");
        await LoadLogsAsync();
        await LoadFooterStatsAsync();
    }

    private void BindHeader()
    {
        if (_container is null)
        {
            return;
        }

        BreadcrumbNameText.Text = _container.Name;
        ContainerNameText.Text = _container.Name;
        ContainerIdText.Text = _container.ShortId;
        ContainerImageText.Text = _container.Image;
        StatusText.Text = _container.DetailsStatusDisplay;
    }

    private void UpdateActionButtons()
    {
        if (_container is null)
        {
            return;
        }

        var running = _container.IsRunning;
        StopButton.IsEnabled = running;
        StartButton.IsEnabled = !running;
        RestartButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }

    private void BreadcrumbContainers_Click(object sender, RoutedEventArgs e) => NavigateBack();

    private void BackButton_Click(object sender, RoutedEventArgs e) => NavigateBack();

    private void NavigateBack()
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            return;
        }

        Frame.Navigate(typeof(ContainersPage));
    }

    private void CopyIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (_container is null || string.IsNullOrWhiteSpace(_container.Id))
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(_container.Id);
        Clipboard.SetContent(package);
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await ContainerCliService.StopContainerAsync(_container.ShortId);
            await RefreshContainerAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error stopping: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await ContainerCliService.StartContainerAsync(_container.ShortId);
            await RefreshContainerAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error starting: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await ContainerCliService.RestartContainerAsync(_container.ShortId);
            await RefreshContainerAsync();
            if (_activeTab == "Logs")
            {
                await LoadLogsAsync();
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error restarting: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await ContainerCliService.DeleteContainerAsync(_container.ShortId);
            NavigateBack();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error deleting: {ex.Message}";
            SetBusy(false);
        }
    }

    private async void TabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tab })
        {
            return;
        }

        SelectTab(tab);
        await LoadActiveTabAsync();
    }

    private void SelectTab(string tab)
    {
        _activeTab = tab;

        LogsPanel.Visibility = tab == "Logs" ? Visibility.Visible : Visibility.Collapsed;
        InspectPanel.Visibility = tab == "Inspect" ? Visibility.Visible : Visibility.Collapsed;
        BindMountsPanel.Visibility = tab == "BindMounts" ? Visibility.Visible : Visibility.Collapsed;
        ExecPanel.Visibility = tab == "Exec" ? Visibility.Visible : Visibility.Collapsed;
        FilesPanel.Visibility = tab == "Files" ? Visibility.Visible : Visibility.Collapsed;
        StatsPanel.Visibility = tab == "Stats" ? Visibility.Visible : Visibility.Collapsed;

        StyleTab(LogsTabLabel, tab == "Logs");
        StyleTab(InspectTabLabel, tab == "Inspect");
        StyleTab(BindMountsTabLabel, tab == "BindMounts");
        StyleTab(ExecTabLabel, tab == "Exec");
        StyleTab(FilesTabLabel, tab == "Files");
        StyleTab(StatsTabLabel, tab == "Stats");
    }

    private static void StyleTab(TextBlock label, bool active)
    {
        label.FontWeight = active ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
        if (active)
        {
            label.Foreground = (Brush)Application.Current.Resources["AppAccentBrush"];
        }
        else
        {
            label.ClearValue(TextBlock.ForegroundProperty);
            label.Opacity = 0.7;
        }

        label.Opacity = active ? 1 : 0.7;
    }

    private async Task LoadActiveTabAsync()
    {
        switch (_activeTab)
        {
            case "Logs":
                await LoadLogsAsync();
                break;
            case "Inspect":
                await LoadInspectAsync();
                break;
            case "Stats":
                await LoadStatsTabAsync();
                break;
        }
    }

    private async Task LoadLogsAsync()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            _fullLogs = await ContainerCliService.GetLogsAsync(_container.ShortId);
            ApplyLogFilter();
            if (_autoScroll)
            {
                ScrollLogsToBottom();
            }
        }
        catch (Exception ex)
        {
            LogsText.Text = $"Error loading logs: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadInspectAsync()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            InspectText.Text = await ContainerCliService.InspectAsync(_container.ShortId);
        }
        catch (Exception ex)
        {
            InspectText.Text = $"Error inspecting container: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadStatsTabAsync()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var stats = await ContainerCliService.GetContainerStatsAsync(_container.ShortId);
            if (stats is null)
            {
                StatsDetailText.Text = "No stats available for this container.";
                return;
            }

            StatsDetailText.Text =
                $"Name: {stats.Name}\n" +
                $"ID: {stats.Id}\n" +
                $"CPU: {stats.CpuPerc}\n" +
                $"Memory: {stats.MemUsage} ({stats.MemPerc})\n" +
                $"Network I/O: {stats.NetIO}\n" +
                $"Block I/O: {stats.BlockIO}\n" +
                $"PIDs: {stats.Pids}";
        }
        catch (Exception ex)
        {
            StatsDetailText.Text = $"Error loading stats: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadFooterStatsAsync()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            var stats = await ContainerCliService.GetContainerStatsAsync(_container.ShortId);
            FooterStatsText.Text = stats?.FooterDisplay ?? "RAM —    CPU —    Block I/O: —";
        }
        catch
        {
            FooterStatsText.Text = "RAM —    CPU —    Block I/O: —";
        }
    }

    private async Task RefreshContainerAsync()
    {
        if (_container is null)
        {
            return;
        }

        var containers = await ContainerCliService.ListContainersAsync();
        var updated = containers.FirstOrDefault(item =>
            string.Equals(item.Id, _container.Id, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.ShortId, _container.ShortId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Name, _container.Name, StringComparison.OrdinalIgnoreCase));

        if (updated is null)
        {
            NavigateBack();
            return;
        }

        _container = updated;
        BindHeader();
        UpdateActionButtons();
        await LoadFooterStatsAsync();
    }

    private void SearchLogsButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBar.Visibility = SearchBar.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (SearchBar.Visibility == Visibility.Visible)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
        else
        {
            SearchBox.Text = string.Empty;
            ApplyLogFilter();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyLogFilter();

    private void ApplyLogFilter()
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            LogsText.Text = string.IsNullOrWhiteSpace(_fullLogs) ? "(no logs)" : _fullLogs;
            return;
        }

        var filtered = _fullLogs
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains(query, StringComparison.OrdinalIgnoreCase));

        LogsText.Text = string.Join(Environment.NewLine, filtered);
    }

    private void CopyLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(LogsText.Text ?? string.Empty);
        Clipboard.SetContent(package);
    }

    private void AutoScrollButton_Click(object sender, RoutedEventArgs e)
    {
        _autoScroll = !_autoScroll;
        AutoScrollButton.Background = _autoScroll
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(0x33, 0x7C, 0x3A, 0xED))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00));

        if (_autoScroll)
        {
            ScrollLogsToBottom();
        }
    }

    private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
    {
        _fullLogs = string.Empty;
        LogsText.Text = string.Empty;
    }

    private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e) => ScrollLogsToBottom();

    private void ScrollLogsToBottom()
    {
        LogsScrollViewer.UpdateLayout();
        LogsScrollViewer.ChangeView(null, LogsScrollViewer.ScrollableHeight, null, true);
    }

    private void SetBusy(bool busy)
    {
        LoadingRing.IsActive = busy;
        LoadingRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        StopButton.IsEnabled = !busy && (_container?.IsRunning ?? false);
        StartButton.IsEnabled = !busy && !(_container?.IsRunning ?? true);
        RestartButton.IsEnabled = !busy;
        DeleteButton.IsEnabled = !busy;
    }
}
