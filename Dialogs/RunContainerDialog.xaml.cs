using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using WslcDesktop.Services;

namespace WslcDesktop.Dialogs;

public sealed partial class RunContainerDialog : UserControl
{
    private readonly string _imageId;
    private readonly List<PortRow> _portRows = [];
    private readonly List<VolumeRow> _volumeRows = [];
    private readonly List<EnvVarRow> _envVarRows = [];
    private TaskCompletionSource<bool>? _completion;

    public RunContainerDialog(string imageReference, string imageId)
    {
        InitializeComponent();
        ImageNameText.Text = imageReference;
        ImageReference = imageReference;
        _imageId = imageId;

        AddVolumeRow(showAddButton: true);
        AddEnvVarRow(showAddButton: true);

        Loaded += RunContainerDialog_Loaded;
        KeyboardAccelerators.Add(new KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.Escape
        });
        KeyboardAccelerators[0].Invoked += (_, args) =>
        {
            args.Handled = true;
            Close(false);
        };
    }

    public string ImageReference { get; }

    public string? ContainerName =>
        string.IsNullOrWhiteSpace(ContainerNameBox.Text) ? null : ContainerNameBox.Text.Trim();

    public async Task<bool> ShowAsync()
    {
        if (App.Window is not MainWindow mainWindow)
        {
            throw new InvalidOperationException("MainWindow is not available.");
        }

        _completion = new TaskCompletionSource<bool>();
        mainWindow.DialogOverlay.Children.Clear();
        mainWindow.DialogOverlay.Children.Add(this);
        mainWindow.DialogOverlay.Visibility = Visibility.Visible;

        return await _completion.Task;
    }

    public IReadOnlyList<(string HostPort, string ContainerPort)> GetPortMappings()
    {
        return _portRows
            .Select(row => (row.HostPortBox.Text.Trim(), row.ContainerPort))
            .Where(p => !string.IsNullOrWhiteSpace(p.Item1) && !string.IsNullOrWhiteSpace(p.Item2))
            .ToList();
    }

    public IReadOnlyList<(string HostPath, string ContainerPath)> GetVolumeMappings()
    {
        return _volumeRows
            .Select(row => (row.HostPathBox.Text.Trim(), row.ContainerPathBox.Text.Trim()))
            .Where(v => !string.IsNullOrWhiteSpace(v.Item1) && !string.IsNullOrWhiteSpace(v.Item2))
            .ToList();
    }

    public IReadOnlyList<(string Name, string Value)> GetEnvironmentVariables()
    {
        return _envVarRows
            .Select(row => (row.NameBox.Text.Trim(), row.ValueBox.Text.Trim()))
            .Where(e => !string.IsNullOrWhiteSpace(e.Item1))
            .ToList();
    }

    private async void RunContainerDialog_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= RunContainerDialog_Loaded;
        await LoadExposedPortsAsync();
    }

    private void Close(bool confirmed)
    {
        if (App.Window is MainWindow mainWindow)
        {
            mainWindow.DialogOverlay.Children.Clear();
            mainWindow.DialogOverlay.Visibility = Visibility.Collapsed;
        }

        _completion?.TrySetResult(confirmed);
        _completion = null;
    }

    private void Backdrop_Tapped(object sender, TappedRoutedEventArgs e) => Close(false);

    private void DialogCard_Tapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;

    private void RunButton_Click(object sender, RoutedEventArgs e) => Close(true);

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close(false);

    private async Task LoadExposedPortsAsync()
    {
        PortsPanel.Children.Clear();
        _portRows.Clear();

        try
        {
            var exposedPorts = await ContainerCliService.GetExposedPortsAsync(_imageId);
            foreach (var containerPort in exposedPorts)
            {
                AddPortRow(containerPort);
            }

            if (exposedPorts.Count == 0)
            {
                PortsPanel.Children.Add(new TextBlock
                {
                    Text = "This image does not expose any ports.",
                    Opacity = 0.7
                });
            }
        }
        catch (Exception ex)
        {
            PortsPanel.Children.Add(new TextBlock
            {
                Text = $"Failed to load exposed ports: {ex.Message}",
                Opacity = 0.7,
                TextWrapping = TextWrapping.WrapWholeWords
            });
        }
    }

    private void AddPortRow(string containerPort)
    {
        var hostBox = new TextBox
        {
            PlaceholderText = "Host port",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var containerBox = new TextBox
        {
            PlaceholderText = "Container port",
            Text = containerPort,
            IsReadOnly = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(containerBox, 1);
        grid.Children.Add(hostBox);
        grid.Children.Add(containerBox);

        PortsPanel.Children.Add(grid);
        _portRows.Add(new PortRow(hostBox, containerPort));
    }

    private void AddVolumeRow(string hostPath = "", string containerPath = "", bool showAddButton = false)
    {
        HideAddButtons(_volumeRows.Select(r => r.AddButton));

        var hostBox = new TextBox
        {
            PlaceholderText = "Host path",
            Text = hostPath,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var browseButton = new Button
        {
            Content = "...",
            Width = 36,
            Height = 32,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = hostBox
        };
        browseButton.Click += BrowseHostPathButton_Click;

        var hostGrid = new Grid { ColumnSpacing = 4 };
        hostGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hostGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(browseButton, 1);
        hostGrid.Children.Add(hostBox);
        hostGrid.Children.Add(browseButton);

        var containerBox = new TextBox
        {
            PlaceholderText = "Container path",
            Text = containerPath,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var addButton = CreateAddButton((_, _) => AddVolumeRow(showAddButton: true));
        addButton.Visibility = showAddButton ? Visibility.Visible : Visibility.Collapsed;

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(containerBox, 1);
        Grid.SetColumn(addButton, 2);
        row.Children.Add(hostGrid);
        row.Children.Add(containerBox);
        row.Children.Add(addButton);

        VolumesPanel.Children.Add(row);
        _volumeRows.Add(new VolumeRow(hostBox, containerBox, addButton));
    }

    private void AddEnvVarRow(string name = "", string value = "", bool showAddButton = false)
    {
        HideAddButtons(_envVarRows.Select(r => r.AddButton));

        var nameBox = new TextBox
        {
            PlaceholderText = "Variable",
            Text = name,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var valueBox = new TextBox
        {
            PlaceholderText = "Value",
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var addButton = CreateAddButton((_, _) => AddEnvVarRow(showAddButton: true));
        addButton.Visibility = showAddButton ? Visibility.Visible : Visibility.Collapsed;

        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(valueBox, 1);
        Grid.SetColumn(addButton, 2);
        grid.Children.Add(nameBox);
        grid.Children.Add(valueBox);
        grid.Children.Add(addButton);

        EnvVarsPanel.Children.Add(grid);
        _envVarRows.Add(new EnvVarRow(nameBox, valueBox, addButton));
    }

    private static Button CreateAddButton(RoutedEventHandler click)
    {
        var button = new Button
        {
            Content = "+",
            FontSize = 18,
            Width = 36,
            Height = 32,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["AccentIconButtonStyle"]
        };
        button.Click += click;
        return button;
    }

    private static void HideAddButtons(IEnumerable<Button> buttons)
    {
        foreach (var button in buttons)
        {
            button.Visibility = Visibility.Collapsed;
        }
    }

    private async void BrowseHostPathButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TextBox hostBox })
        {
            return;
        }

        var folderPicker = new FolderPicker(App.Window.AppWindow.Id);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is not null)
        {
            hostBox.Text = folder.Path;
        }
    }

    private sealed record PortRow(TextBox HostPortBox, string ContainerPort);
    private sealed record VolumeRow(TextBox HostPathBox, TextBox ContainerPathBox, Button AddButton);
    private sealed record EnvVarRow(TextBox NameBox, TextBox ValueBox, Button AddButton);
}
