using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MessagePublisher.Services;
using MessagePublisher.ViewModels;

namespace MessagePublisher;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly JsonMessageSerializer _serializer = new();
    private MessagePackageLoadContext? _loadContext;

    private string _feedUrl = "https://api.nuget.org/v3/index.json";
    private string _packageId = "";
    private string _packageVersion = "";
    private string _baseTypeFullName = "";
    private string _username = "";
    private string _password = "";

    private string _statusText = "Ready.";
    private string _serializedPreview = "";
    private bool _isBusy;

    public MainViewModel()
    {
        MessageTypes = new ObservableCollection<string>();
        PropertyEditors = new ObservableCollection<PropertyEditorViewModel>();

        LoadPackageCommand = new RelayCommand(async _ => await LoadPackageAsync(), _ => !IsBusy);
        BuildSerializedCommand = new RelayCommand(_ => BuildSerialized(), _ => !IsBusy && SelectedMessageTypeIndex >= 0 && !PropertyEditors.Any(p => p.HasErrors));
    }

    public string FeedUrl
    {
        get => _feedUrl;
        set { if (SetField(ref _feedUrl, value)) OnPropertyChanged(nameof(FeedUrl)); }
    }

    public string PackageId
    {
        get => _packageId;
        set { if (SetField(ref _packageId, value)) OnPropertyChanged(nameof(PackageId)); }
    }

    public string PackageVersion
    {
        get => _packageVersion;
        set { if (SetField(ref _packageVersion, value)) OnPropertyChanged(nameof(PackageVersion)); }
    }

    /// <summary>
    /// Full name of the base message type (e.g. MyCompany.Messages.MessageBase).
    /// </summary>
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
            if (string.IsNullOrWhiteSpace(PackageId))
                throw new InvalidOperationException("Package id is required.");
            if (string.IsNullOrWhiteSpace(PackageVersion))
                throw new InvalidOperationException("Package version is required.");
            if (string.IsNullOrWhiteSpace(BaseTypeFullName))
                throw new InvalidOperationException("Base type full name is required.");

            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MessagePublisher",
                "packages",
                Sanitize(PackageId),
                Sanitize(PackageVersion));

            StatusText = "Downloading package...";
            var extracted = await NuGetPackageLoader.DownloadAndExtractAsync(
                FeedUrl.Trim(),
                PackageId.Trim(),
                PackageVersion.Trim(),
                root,
                string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                string.IsNullOrWhiteSpace(Password) ? null : Password);

            var lib = PackageAssemblyResolver.ResolveLibDirectory(extracted)
                ?? throw new InvalidOperationException("No suitable lib/TFM folder found in the package.");

            StatusText = $"Loaded lib from: {lib}. Discovering types...";

            var discovered = MessageTypeDiscovery.Discover(lib, BaseTypeFullName.Trim(), out var ctx);
            _loadContext = ctx;
            _discovered = discovered.ToList();

            foreach (var d in _discovered)
                MessageTypes.Add(d.DisplayName);

            StatusText = discovered.Count == 0
                ? "No concrete message types found."
                : $"Found {discovered.Count} message type(s). Select one to edit.";
        }
        catch (Exception ex)
        {
            StatusText = "Error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
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

    private void Editor_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e) =>
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
