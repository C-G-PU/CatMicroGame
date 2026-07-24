using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DesktopCat.Services
{
    public class NotificationService
    {
        private UserNotificationListener? _listener;

        // Событие, которое будет вызываться при новом уведомлении (AppName, Title, Message)
        public event Action<string, string, string>? OnNotificationReceived;

        public NotificationService()
        {
        }

        public async Task<bool> InitializeAsync()
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
            {
                return false; // ОС не поддерживает
            }

            _listener = UserNotificationListener.Current;
            var accessStatus = await _listener.RequestAccessAsync();

            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                return false; // Нет доступа (пользователь не разрешил)
            }

            // Подписываемся на события добавления уведомлений
            _listener.NotificationChanged += Listener_NotificationChanged;

            return true;
        }

        private async void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            // Нас интересуют только новые добавленные уведомления
            if (args.ChangeKind != UserNotificationChangedKind.Added)
                return;

            try
            {
                // Получаем само уведомление по его ID
                var notification = _listener?.GetNotification(args.UserNotificationId);
                if (notification == null) return;

                // Извлекаем имя приложения
                string appName = notification.AppInfo.DisplayInfo.DisplayName;

                // Извлекаем текст
                var bindings = notification.Notification.Visual.Bindings;
                string title = "";
                string message = "";

                if (bindings.Count > 0)
                {
                    var textElements = bindings[0].GetTextElements();
                    if (textElements.Count > 0)
                        title = textElements[0].Text;
                    if (textElements.Count > 1)
                        message = textElements[1].Text;
                }

                // Вызываем событие в приложении
                OnNotificationReceived?.Invoke(appName, title, message);
            }
            catch
            {
                // Игнорируем ошибки при чтении конкретного уведомления (иногда они удаляются быстрее, чем мы их читаем)
            }
        }
    }
}
