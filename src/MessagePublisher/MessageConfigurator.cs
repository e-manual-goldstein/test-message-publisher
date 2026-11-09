using System;
using System.Collections.Generic;
using MessagePublisher.ViewModels;

namespace MessagePublisher
{
    internal static class MessageConfigurator
    {
        /// <summary>Message property name that feeds <see cref="MessageLabels.ProcessName"/>.</summary>
        internal const string ProcessPropertyName = "Process";

        /// <summary>
        /// <see cref="MessageLabels.RoutingKey"/> comes from the host UI;
        /// <see cref="MessageLabels.ProcessName"/> from message property <see cref="ProcessPropertyName"/>;
        /// <see cref="MessageLabels.SystemId"/> from message property <c>SystemId</c>.
        /// </summary>
        internal static MessageLabels GetAddtionalMessageLabels(
            string routingKey,
            IEnumerable<PropertyEditorViewModel> propertyEditors)
        {
            if (propertyEditors == null)
                throw new ArgumentNullException(nameof(propertyEditors));

            return new MessageLabels
            {
                ProcessName = GetEditorString(propertyEditors, ProcessPropertyName),
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
