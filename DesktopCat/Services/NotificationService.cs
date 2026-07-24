using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Threading;
using System.Diagnostics;
using System;

namespace DesktopCat.Services
{
    public class NotificationService
    {
        public event Action<string, string, string>? OnNotificationReceived;
        private AutomationEventHandler? _windowOpenedHandler;

        public NotificationService()
        {
        }

        public Task<bool> InitializeAsync()
        {
            try
            {
                // Подписываемся на события появления новых окон
                _windowOpenedHandler = new AutomationEventHandler(OnWindowOpened);
                Automation.AddAutomationEventHandler(
                    WindowPattern.WindowOpenedEvent,
                    AutomationElement.RootElement,
                    TreeScope.Children,
                    _windowOpenedHandler);

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private void OnWindowOpened(object sender, AutomationEventArgs e)
        {
            if (sender is AutomationElement element)
            {
                try
                {
                    // Проверяем, является ли окно тост-уведомлением
                    if (element.Current.ClassName == "Windows.UI.Core.CoreWindow" && element.Current.Name.Contains("Toast"))
                    {
                        ParseNotification(element);
                    }
                }
                catch (ElementNotAvailableException)
                {
                    // Игнорируем, окно могло уже закрыться
                }
                catch
                {
                    // Игнорируем другие ошибки при чтении Automation свойств
                }
            }
        }

        private void ParseNotification(AutomationElement toastElement)
        {
            // Обходим внутренние элементы окна для поиска текста
            var walker = TreeWalker.ControlViewWalker;
            string appName = "Уведомление";
            string title = "";
            string message = "";

            var child = walker.GetFirstChild(toastElement);
            List<string> textLines = new List<string>();

            ExtractText(child, walker, textLines);

            if (textLines.Count > 0)
            {
                // Обычно первый элемент - название приложения, второй - заголовок, третий - текст
                appName = textLines[0];
                if (textLines.Count > 1) title = textLines[1];
                if (textLines.Count > 2) message = textLines[2];
                else if (textLines.Count > 1) { message = title; title = ""; }

                TriggerNotification(appName, title, message);
            }
        }

        private void ExtractText(AutomationElement node, TreeWalker walker, List<string> output)
        {
            if (node == null) return;

            try
            {
                if (node.Current.ControlType == ControlType.Text)
                {
                    string text = node.Current.Name;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        output.Add(text.Trim());
                    }
                }
            }
            catch (ElementNotAvailableException) {}

            var child = walker.GetFirstChild(node);
            while (child != null)
            {
                ExtractText(child, walker, output);
                try { child = walker.GetNextSibling(child); } catch { break; }
            }
        }

        protected void TriggerNotification(string appName, string title, string message)
        {
            OnNotificationReceived?.Invoke(appName, title, message);
        }
    }
}
