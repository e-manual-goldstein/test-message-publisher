using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MessagePublisher.Composition;
using MessagePublisher.Services;
using MessagePublisher.ViewModels;

namespace MessagePublisher;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppServiceCollection _serviceCollection;
    private readonly IMessageSerializer _serializer = ModuleCompositionRoot.CreateSerializer();
    
    private string _statusText = "Ready.";
    private string _serializedPreview = "";
    private bool _isBusy;
    private string _messageTypeFilter = "";
    private MessageTypeListItem? _selectedMessageTypeItem;
    private string _routingKey = "";
    private bool _fillDefaultValues;

      /// <summary>Root provider built during construction from <see cref="_serviceCollection"/>.</summary>
    public AppServiceProvider ServiceProvider { get; }

    /// <summary>Service registrations (configure before <see cref="AppServiceCollection.BuildServiceProvider"/>).</summary>
    public AppServiceCollection ServiceCollection => _serviceCollection;

    public MainViewModel()
    {
        _serviceCollection = new AppServiceCollection();
        ServiceConfigurator.Configure(_serviceCollection);
        ServiceProvider = _serviceCollection.BuildServiceProvider();

        FilteredMessageTypes = new ObservableCollection<MessageTypeListItem>();
        PropertyEditors = new ObservableCollection<PropertyEditorViewModel>();

        BuildSerializedCommand = new RelayCommand(
            _ => BuildSerialized(),
            _ => !IsBusy && HasRoutingKey && SelectedMessageTypeItem != null && !PropertyEditors.Any(p => p.HasErrors));
        SendPayloadCommand = new RelayCommand(_ => SendPayload(), _ => CanSendPayload());
    }

    public string RoutingKey
    {
        get => _routingKey;
        set
        {
            if (!SetField(ref _routingKey, value))
                return;
            OnPropertyChanged(nameof(RoutingKey));
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SendPayloadCommand).RaiseCanExecuteChanged();
        }
    }

    private bool HasRoutingKey => !string.IsNullOrWhiteSpace(RoutingKey);

    public bool FillDefaultValues
    {
        get => _fillDefaultValues;
        set
        {
            if (SetField(ref _fillDefaultValues, value))
                OnPropertyChanged(nameof(FillDefaultValues));
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set { if (SetField(ref _statusText, value)) OnPropertyChanged(nameof(StatusText)); }
    }

    public string SerializedPreview
    {
        get => _serializedPreview;
        private set
        {
            if (!SetField(ref _serializedPreview, value))
                return;
            OnPropertyChanged(nameof(SerializedPreview));
            ((RelayCommand)SendPayloadCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value))
                return;
            OnPropertyChanged(nameof(IsBusy));
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SendPayloadCommand).RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<MessageTypeListItem> FilteredMessageTypes { get; }

    public string MessageTypeFilter
    {
        get => _messageTypeFilter;
        set
        {
            if (!SetField(ref _messageTypeFilter, value))
                return;
            OnPropertyChanged(nameof(MessageTypeFilter));
            ApplyMessageTypeFilter();
        }
    }

    public MessageTypeListItem? SelectedMessageTypeItem
    {
        get => _selectedMessageTypeItem;
        set
        {
            if (!SetField(ref _selectedMessageTypeItem, value))
                return;
            OnPropertyChanged(nameof(SelectedMessageTypeItem));
            OnMessageTypeSelectionChanged();
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
        }
    }

    private List<DiscoveredMessageType> _discovered = new();

    public ObservableCollection<PropertyEditorViewModel> PropertyEditors { get; }

    public ICommand BuildSerializedCommand { get; }
    public ICommand SendPayloadCommand { get; }

    public void Initialize()
    {
        LoadMessageTypes();
    }

    private void LoadMessageTypes()
    {
        IsBusy = true;
        SerializedPreview = string.Empty;
        PropertyEditors.Clear();
        _discovered.Clear();
        FilteredMessageTypes.Clear();
        MessageTypeFilter = string.Empty;
        SelectedMessageTypeItem = null;

        try
        {
            var discovered = ReferencedModuleDiscovery.Discover(typeof(Event).FullName);
            _discovered = discovered.ToList();
            ApplyMessageTypeFilter();

            StatusText = discovered.Count == 0
                ? "No message types found in referenced MessageTypes project."
                : $"Loaded {discovered.Count} message type(s). Select one to edit.";
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

        var selected = SelectedMessageTypeItem?.Discovered;
        if (selected is null)
        {
            ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
            return;
        }

        var type = selected.ClrType;
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
            var selected = SelectedMessageTypeItem?.Discovered;
            if (selected is null)
                return;

            if (FillDefaultValues)
            {
                foreach (var editor in PropertyEditors)
                    editor.ApplyDefaultValueIfEmpty();
                ((RelayCommand)BuildSerializedCommand).RaiseCanExecuteChanged();
            }

            var type = selected.ClrType;
            var instance = MessageInstanceBuilder.Build(type, PropertyEditors);
            var labels = MessageConfigurator.GetAddtionalMessageLabels(RoutingKey, PropertyEditors);
            SerializedPreview = _serializer.Serialize(instance, labels);
            StatusText = "Serialized preview updated.";
        }
        catch (Exception ex)
        {
            StatusText = "Serialization error: " + ex.Message;
        }
    }

    private bool CanSendPayload() =>
        !IsBusy && HasRoutingKey && !string.IsNullOrWhiteSpace(SerializedPreview);

    private void SendPayload()
    {
        try
        {
            MsgRouteEngine.RouteMessage(RoutingKey, SerializedPreview);
            StatusText = "Message sent via routing engine.";
        }
        catch (Exception ex)
        {
            StatusText = "Send error: " + ex.Message;
        }
    }

    private void ApplyMessageTypeFilter()
    {
        var previous = SelectedMessageTypeItem;
        FilteredMessageTypes.Clear();

        var filter = MessageTypeFilter.Trim();
        foreach (var d in _discovered)
        {
            if (filter.Length == 0
                || d.ShortName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                FilteredMessageTypes.Add(new MessageTypeListItem(d));
            }
        }

        if (previous is not null)
        {
            var match = FilteredMessageTypes.FirstOrDefault(i => i.Discovered.ClrType == previous.Discovered.ClrType);
            SelectedMessageTypeItem = match;
        }
        else
        {
            SelectedMessageTypeItem = null;
        }
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
