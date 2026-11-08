using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePublisher.ViewModels;

namespace MessagePublisher.Services;

/// <summary>
/// Creates a message instance and applies values from the dynamic form.
/// </summary>
public static class MessageInstanceBuilder
{
    public static object Build(Type messageType, IEnumerable<PropertyEditorViewModel> editors)
    {
        var instance = Activator.CreateInstance(messageType)
            ?? throw new InvalidOperationException($"Cannot create instance of {messageType.FullName}.");

        foreach (var editor in editors)
        {
            if (!editor.CanEdit)
                continue;

            var prop = messageType.GetProperty(editor.PropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
                continue;

            object? value = editor.ParseToPropertyType(prop.PropertyType);
            prop.SetValue(instance, value);
        }

        return instance;
    }

    /// <summary>
    /// Parses editor input into the target property type (handles nullable).
    /// </summary>
    public static object? ParseValue(PropertyEditorViewModel editor, Type propertyType)
    {
        return editor.ParseToPropertyType(propertyType);
    }
}
