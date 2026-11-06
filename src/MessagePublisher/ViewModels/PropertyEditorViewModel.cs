using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MessagePublisher.ViewModels;

/// <summary>
/// One editable property row: label, editor kind, raw text, validation.
/// </summary>
public sealed class PropertyEditorViewModel : INotifyDataErrorInfo, INotifyPropertyChanged
{
    private static readonly char[] HexSeparators = [' ', '-', ','];

    private string _valueText = string.Empty;
    private bool? _boolValue;
    private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);

    public PropertyEditorViewModel(PropertyInfo property)
    {
        Property = property;
        PropertyName = property.Name;
        Label = property.Name;
        PropertyType = property.PropertyType;
        EditorKind = MapKind(property.PropertyType);
        CanEdit = EditorKind != PropertyEditorKind.Unsupported && property.CanWrite;
        if (EditorKind == PropertyEditorKind.Boolean)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                _boolValue = null;
                _valueText = string.Empty;
            }
            else
            {
                _boolValue = false;
                _valueText = bool.FalseString;
            }
        }
    }

    public PropertyInfo Property { get; }

    public string PropertyName { get; }

    public string Label { get; }

    public Type PropertyType { get; }

    public PropertyEditorKind EditorKind { get; }

    public bool CanEdit { get; }

    public bool IsBooleanEditor => EditorKind == PropertyEditorKind.Boolean;

    /// <summary>First validation error for display in the UI.</summary>
    public string? ErrorMessage => _errors.Values.FirstOrDefault();

    public bool? BoolValue
    {
        get => _boolValue;
        set
        {
            if (_boolValue == value)
                return;
            _boolValue = value;
            _valueText = value == true ? bool.TrueString : bool.FalseString;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValueText));
            ClearError(nameof(ValueText));
            ClearError(nameof(BoolValue));
        }
    }

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (_valueText == value)
                return;
            _valueText = value;
            OnPropertyChanged();
            ClearError(nameof(ValueText));
            ValidateCurrent();
        }
    }

    public object? ParseToPropertyType(Type targetType)
    {
        var (underlying, isNullable) = Nullable.GetUnderlyingType(targetType) is { } u
            ? (u, true)
            : (targetType, false);

        if (EditorKind == PropertyEditorKind.Boolean)
        {
            if (isNullable && BoolValue is null)
                return null;
            return BoolValue ?? false;
        }

        if (isNullable && string.IsNullOrWhiteSpace(ValueText))
            return null;

        try
        {
            return underlying switch
            {
                _ when underlying == typeof(string) => ValueText,
                _ when underlying == typeof(bool) => ParseBool(ValueText),
                _ when underlying == typeof(byte) => byte.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(sbyte) => sbyte.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(short) => short.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(ushort) => ushort.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(int) => int.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(uint) => uint.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(long) => long.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(ulong) => ulong.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(decimal) => decimal.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(double) => double.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(float) => float.Parse(ValueText.Trim(), CultureInfo.InvariantCulture),
                _ when underlying == typeof(Guid) => Guid.Parse(ValueText.Trim()),
                _ when underlying == typeof(DateTime) => DateTime.Parse(ValueText.Trim(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces),
                _ when underlying == typeof(DateTimeOffset) => DateTimeOffset.Parse(ValueText.Trim(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces),
                _ when underlying == typeof(byte[]) => ParseByteArray(ValueText),
                _ when underlying.IsEnum => Enum.Parse(underlying, ValueText.Trim(), ignoreCase: true),
                _ => throw new NotSupportedException($"Unsupported property type: {underlying.FullName}")
            };
        }
        catch (Exception ex)
        {
            SetError(nameof(ValueText), ex.Message);
            throw;
        }
    }

    private static object ParseBool(string s)
    {
        var t = s.Trim();
        if (bool.TryParse(t, out var b))
            return b;
        if (int.TryParse(t, out var n))
            return n != 0;
        throw new FormatException("Expected true/false or 0/1.");
    }

    private static byte[] ParseByteArray(string text)
    {
        var t = text.Trim();
        if (t.Length == 0)
            return Array.Empty<byte>();

        // Base64
        if (IsLikelyBase64(t))
        {
            try
            {
                return Convert.FromBase64String(t);
            }
            catch (FormatException)
            {
                // fall through to hex
            }
        }

        // Hex: optional 0x, separators
        var hex = t.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? t[2..] : t;
        var sep = new HashSet<char>(HexSeparators);
        hex = new string(hex.Where(c => !sep.Contains(c)).ToArray());
        if (hex.Length % 2 != 0)
            throw new FormatException("Hex length must be even (or use Base64).");

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    private static bool IsLikelyBase64(string t)
    {
        if (t.Length < 4)
            return false;
        if (t.Any(c => char.IsWhiteSpace(c)))
            return false;
        return t.All(c => char.IsLetterOrDigit(c) or c is '+' or '/' or '=');
    }

    private void ValidateCurrent()
    {
        if (!CanEdit)
            return;

        try
        {
            _ = ParseToPropertyType(PropertyType);
            ClearError(nameof(ValueText));
        }
        catch (Exception ex)
        {
            SetError(nameof(ValueText), ex.Message);
        }
    }

    public static PropertyEditorKind MapKind(Type type)
    {
        var u = Nullable.GetUnderlyingType(type) ?? type;

        if (u == typeof(string))
            return PropertyEditorKind.String;
        if (u == typeof(bool))
            return PropertyEditorKind.Boolean;
        if (u == typeof(byte) || u == typeof(sbyte) || u == typeof(short) || u == typeof(ushort)
            || u == typeof(int) || u == typeof(uint) || u == typeof(long) || u == typeof(ulong))
            return PropertyEditorKind.Integer;
        if (u == typeof(decimal))
            return PropertyEditorKind.Decimal;
        if (u == typeof(double) || u == typeof(float))
            return PropertyEditorKind.Floating;
        if (u == typeof(Guid))
            return PropertyEditorKind.Guid;
        if (u == typeof(DateTime))
            return PropertyEditorKind.DateTime;
        if (u == typeof(DateTimeOffset))
            return PropertyEditorKind.DateTimeOffset;
        if (u == typeof(byte[]))
            return PropertyEditorKind.ByteArray;
        if (u.IsEnum)
            return PropertyEditorKind.Enum;

        return PropertyEditorKind.Unsupported;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is null)
            return _errors.Values.ToList();
        return _errors.TryGetValue(propertyName, out var e) ? new[] { e } : Array.Empty<string>();
    }

    private void SetError(string propertyName, string message)
    {
        _errors[propertyName] = message;
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasErrors));
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ClearError(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasErrors));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
