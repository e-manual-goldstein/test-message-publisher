using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePublisher.ViewModels;

namespace MessagePublisher
{
    internal static class MessageConfigurator
    {
        /// <summary>
        /// Reads <see cref="MessageLabels"/> fields from the dynamic form (matched by property name, case-insensitive).
        /// </summary>
        internal static MessageLabels GetAddtionalMessageLabels(IEnumerable<PropertyEditorViewModel> propertyEditors)
        {
            if (propertyEditors == null)
                throw new ArgumentNullException(nameof(propertyEditors));

            var labels = new MessageLabels();

            foreach (var labelProperty in typeof(MessageLabels).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (labelProperty.PropertyType != typeof(string) || !labelProperty.CanWrite)
                    continue;

                var value = GetEditorString(propertyEditors, labelProperty.Name);
                labelProperty.SetValue(labels, value, null);
            }

            return labels;
        }

        private static string GetEditorString(IEnumerable<PropertyEditorViewModel> propertyEditors, string propertyName)
        {
            foreach (var editor in propertyEditors)
            {
                if (!editor.CanEdit)
                    continue;
                if (string.Equals(editor.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
                    return editor.ValueText ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
