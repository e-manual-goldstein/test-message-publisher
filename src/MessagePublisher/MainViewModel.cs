using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MessagePublisher.Configuration;
using MessagePublisher.Services;
using MessagePublisher.ViewModels;
using Microsoft.Extensions.Configuration;

namespace MessagePublisher;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly JsonMessageSerializer _serializer = new();
    private readonly NuGetPackageSettings _packageSettings;
    private MessagePackageLoadContext? _loadContext;

    private NuGetFeedOption? _selectedFeed;
    private string _messageTypesPackageId = "";
    private string _baseTypePackageId = "";
    private string _baseTypeFullName = "";
    private string? _selectedMessagePackageVersion;
    private string? _selectedBasePackageVersion;
    private string _username = "";
    private string _password = "";

    private string _statusText = "Ready.";
    private string _serializedPreview = "";
    private bool _isBusy;

    public MainViewModel()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _packageSettings = new NuGetPackageSettings();
        configuration.GetSection(NuGetPackageSettings.SectionName).Bind(_packageSettings);

        MessageTypesPackageId = _packageSettings.MessageTypesPackageId;
        BaseTypePackageId = _packageSettings.BaseTypePackageId;
        BaseTypeFullName = _packageSettings.BaseTypeFullName;

        FeedSources = new ObservableCollection<NuGetFeedOption>();
        MessagePackageVersions = new ObservableCollection<string>();
        BasePackageVersions = new ObservableCollection<string>();
        MessageTypes = new ObservableCollection<string>();
        PropertyEditors = new ObservableCollection<PropertyEditorViewModel>();

        LoadPackageCommand = new RelayCommand(async _ => await LoadPackageAsync(), _ => CanLoadPackages());
        BuildSerializedCommand = new RelayCommand(_ => BuildSerialized(), _ => !IsBusy && SelectedMessageTypeIndex >= 0 && !PropertyEditors.Any(p => p.HasErrors));
        RefreshVersionsCommand = new RelayCommand(async _ => await RefreshVersionsAsync(), _ => !IsBusy && SelectedFeed != null);
    }

    public ObservableCollection<NuGetFeedOption> FeedSources { get; }

    public NuGetFeedOption? SelectedFeed
    {
        get => _selectedFeed;
        set
        {
            if (!SetField(ref _selectedFeed, value))
                return;
            OnPropertyChanged(nameof(SelectedFeed));
            ((RelayCommand)LoadPackageCommand).RaiseCanExecuteChanged();
            _ = RefreshVersionsAsync();
        }
    }

    /// <summary>Package A — message type definitions.</summary>
    public string MessageTypesPackageId
    {
        get => _messageTypesPackageId;
        set { if (SetField(ref _messageTypesPackageId, value)) OnPropertyChanged(nameof(MessageTypesPackageId)); }
    }

    /// <summary>Package B — base type assembly.</summary>
    public string BaseTypePackageId
    {
        get => _baseTypePackageId;
        set { if (SetField(ref _baseTypePackageId, value)) OnPropertyChanged(nameof(BaseTypePackageId)); }
    }

    public ObservableCollection<string> MessagePackageVersions { get; }
    public ObservableCollection<string> BasePackageVersions { get; }

    public string? SelectedMessagePackageVersion
    {
        get => _selectedMessagePackageVersion;
        set
        {
            if (!SetField(ref _selectedMessagePackageVersion, value))
                return;
            OnPropertyChanged(nameof(SelectedMessagePackageVersion));
            ((RelayCommand)LoadPackageCommand).RaiseCanExecuteChanged();
        }
    }

    public string? SelectedBasePackageVersion
    {
        get => _selectedBasePackageVersion;
        set
        {
            if (!SetField(ref _selectedBasePackageVersion, value))
                return;
            OnPropertyChanged(nameof(SelectedBasePackageVersion));
            ((RelayCommand)LoadPackageCommand).RaiseCanExecuteChanged();
        }
    }

    public string BaseTypeFullName
    {
        get => _baseTypeFullName;
        set { if (SetField(ref _baseTypeFullName, value)) OnPropertyChanged(nameof(BaseTypeFullName)); }
    }

    public string Username
    {
        get => _username;
        set { if (SetField(ref _username, value)) OnPropertyChanged(nameof(Username)); }
    }

    public string Password
    {
        get => _password;
        set { if (SetField(ref _password, value)) OnPropertyChanged(nameof(Password)); }
    }

    public string StatusText
    {
        get => _statusText;
        private set { if (SetField(ref _statusText, value)) OnPropertyChanged(nameof(StatusText)); }
    }

    public string SerializedPreview
    {
        get => _serializedPreview;
        private set { if (SetField(ref _serializedPreview, value)) OnPropertyChanged(nameof(SerializedPreview)); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value))
                return;
            OnPropertyChanged(nameof(IsBusy));
            ((RelayCommand)LoadPackageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RefreshVersionsCommand).RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<string> MessageTypes { get; }

    private int _selectedMessageTypeIndex = -1;
    public int SelectedMessageTypeIndex
    {
        get => _selectedMessageTypeIndex;
        set
        {
            if (!SetField(ref _selectedMessageTypeIndex, value))
                return;
            OnMessageTypeSelectionChanged();
            OnPropertyChanged(nameof(SelectedMessageTypeIndex));
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
        }
    }

    private List<DiscoveredMessageType> _discovered = new();

    public ObservableCollection<PropertyEditorViewModel> PropertyEditors { get; }

    public ICommand LoadPackageCommand { get; }
    public ICommand BuildSerializedCommand { get; }
    public ICommand RefreshVersionsCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            FeedSources.Clear();
            foreach (var feed in NuGetConfigFeedProvider.GetEnabledFeeds())
                FeedSources.Add(feed);

            if (FeedSources.Count == 0)
            {
                StatusText = "No enabled NuGet sources found in NuGet.config.";
                return;
            }

            var preferred = _packageSettings.PreferredFeedName;
            SelectedFeed = FeedSources.FirstOrDefault(f =>
                               string.Equals(f.Name, preferred, StringComparison.OrdinalIgnoreCase))
                           ?? FeedSources[0];

            StatusText = $"Loaded {FeedSources.Count} NuGet source(s) from NuGet.config.";
            await RefreshVersionsAsync();
        }
        catch (Exception ex)
        {
            StatusText = "Startup error: " + ex.Message;
        }
    }

    public async Task RefreshVersionsAsync()
    {
        if (SelectedFeed is null)
            return;

        IsBusy = true;
        try
        {
            StatusText = "Querying package versions from feed...";

            var messageTask = NuGetPackageVersionProvider.GetVersionsAsync(SelectedFeed, MessageTypesPackageId);
            var baseTask = NuGetPackageVersionProvider.GetVersionsAsync(SelectedFeed, BaseTypePackageId);
            await Task.WhenAll(messageTask, baseTask);

            ReplaceCollection(MessagePackageVersions, messageTask.Result);
            ReplaceCollection(BasePackageVersions, baseTask.Result);

            SelectedMessagePackageVersion = PickVersion(SelectedMessagePackageVersion, MessagePackageVersions);
            SelectedBasePackageVersion = PickVersion(SelectedBasePackageVersion, BasePackageVersions);

            StatusText = $"Versions loaded (A: {MessagePackageVersions.Count}, B: {BasePackageVersions.Count}).";
        }
        catch (Exception ex)
        {
            StatusText = "Version query error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLoadPackages() =>
        !IsBusy
        && SelectedFeed != null
        && !string.IsNullOrWhiteSpace(SelectedMessagePackageVersion)
        && !string.IsNullOrWhiteSpace(SelectedBasePackageVersion);

    private async Task LoadPackageAsync()
    {
        IsBusy = true;
        SerializedPreview = string.Empty;
        MessageTypes.Clear();
        PropertyEditors.Clear();
        _discovered.Clear();
        SelectedMessageTypeIndex = -1;
        _loadContext = null;

        try
        {
            if (SelectedFeed is null)
                throw new InvalidOperationException("Select a NuGet feed.");
            if (string.IsNullOrWhiteSpace(MessageTypesPackageId) || string.IsNullOrWhiteSpace(BaseTypePackageId))
                throw new InvalidOperationException("Package ids must be configured in appsettings.json.");
            if (string.IsNullOrWhiteSpace(SelectedMessagePackageVersion) || string.IsNullOrWhiteSpace(SelectedBasePackageVersion))
                throw new InvalidOperationException("Select a version for both packages.");
            if (string.IsNullOrWhiteSpace(BaseTypeFullName))
                throw new InvalidOperationException("Base type full name is required.");

            var cacheRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MessagePublisher",
                "packages");

            var baseRoot = Path.Combine(cacheRoot, Sanitize(BaseTypePackageId), Sanitize(SelectedBasePackageVersion!));
            var messageRoot = Path.Combine(cacheRoot, Sanitize(MessageTypesPackageId), Sanitize(SelectedMessagePackageVersion!));

            var user = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim();
            var pass = string.IsNullOrWhiteSpace(Password) ? null : Password;

            StatusText = "Downloading package B (base type)...";
            var extractedBase = await NuGetPackageLoader.DownloadAndExtractAsync(
                SelectedFeed,
                BaseTypePackageId.Trim(),
                SelectedBasePackageVersion!,
                baseRoot,
                user,
                pass);

            StatusText = "Downloading package A (message types)...";
            var extractedMessages = await NuGetPackageLoader.DownloadAndExtractAsync(
                SelectedFeed,
                MessageTypesPackageId.Trim(),
                SelectedMessagePackageVersion!,
                messageRoot,
                user,
                pass);

            var baseLib = PackageAssemblyResolver.ResolveLibDirectory(extractedBase)
                ?? throw new InvalidOperationException("No lib/TFM folder in base type package B.");
            var messageLib = PackageAssemblyResolver.ResolveLibDirectory(extractedMessages)
                ?? throw new InvalidOperationException("No lib/TFM folder in message types package A.");

            StatusText = "Discovering message types...";
            var discovered = MessageTypeDiscovery.Discover(messageLib, baseLib, BaseTypeFullName.Trim(), out var ctx);
            _loadContext = ctx;
            _discovered = discovered.ToList();

            foreach (var d in _discovered)
                MessageTypes.Add(d.DisplayName);

            StatusText = discovered.Count == 0
                ? "No concrete message types found in package A."
                : $"Found {discovered.Count} message type(s). Select one to edit.";
        }
        catch (Exception ex)
        {
            StatusText = "Error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            ((RelayCommand)LoadPackageCommand).RaiseCanExecuteChanged();
        }
    }

    private void OnMessageTypeSelectionChanged()
    {
        foreach (var e in PropertyEditors.ToList())
            e.ErrorsChanged -= Editor_ErrorsChanged;

        PropertyEditors.Clear();

        if (SelectedMessageTypeIndex < 0 || SelectedMessageTypeIndex >= _discovered.Count)
        {
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
            return;
        }

        var type = _discovered[SelectedMessageTypeIndex].ClrType;
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!prop.CanRead)
                continue;
            var editor = new PropertyEditorViewModel(prop);
            if (!editor.CanEdit)
                continue;
            editor.ErrorsChanged += Editor_ErrorsChanged;
            PropertyEditors.Add(editor);
        }

        ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
    }

    private void Editor_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e) =>
        ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();

    private void BuildSerialized()
    {
        try
        {
            if (SelectedMessageTypeIndex < 0 || SelectedMessageTypeIndex >= _discovered.Count)
                return;

            var type = _discovered[SelectedMessageTypeIndex].ClrType;
            var instance = MessageInstanceBuilder.Build(type, PropertyEditors);
            SerializedPreview = _serializer.Serialize(instance, type);
            StatusText = "Serialized preview updated.";
        }
        catch (Exception ex)
        {
            StatusText = "Serialization error: " + ex.Message;
        }
    }

    private static void ReplaceCollection(ObservableCollection<string> target, IReadOnlyList<string> values)
    {
        target.Clear();
        foreach (var v in values)
            target.Add(v);
    }

    private static string? PickVersion(string? current, ObservableCollection<string> available)
    {
        if (available.Count == 0)
            return null;
        if (!string.IsNullOrWhiteSpace(current) && available.Contains(current))
            return current;
        return available[0];
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        return true;
    }
}
