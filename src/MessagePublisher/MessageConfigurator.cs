using System;
using System.Collections.Generic;
using MessagePublisher.ViewModels;

namespace MessagePublisher
{
    internal static class MessageConfigurator
    {
        /// <summary>
        /// <see cref="MessageLabels.ProcessName"/> and <see cref="MessageLabels.RoutingKey"/> come from the host UI;
        /// <see cref="MessageLabels.SystemId"/> is read from the selected message's property editor.
        /// </summary>
        internal static MessageLabels GetAddtionalMessageLabels(
            string processName,
            string routingKey,
            IEnumerable<PropertyEditorViewModel> propertyEditors)
        {
            if (propertyEditors == null)
                throw new ArgumentNullException(nameof(propertyEditors));

            return new MessageLabels
            {
                ProcessName = processName ?? string.Empty,
                RoutingKey = routingKey ?? string.Empty,
                SystemId = GetEditorString(propertyEditors, nameof(MessageLabels.SystemId))
            };
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
