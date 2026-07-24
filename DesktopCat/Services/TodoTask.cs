using System;

namespace DesktopCat.Services
{
    public class TodoTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";

        // Запланированное время. По умолчанию DateTime.MinValue (без времени)
        public DateTime ScheduledTime { get; set; } = DateTime.MinValue;

        // Постоянная/закрепленная задача, которая не удаляется после завершения дня
        public bool IsPermanent { get; set; } = false;

        // Выполнена ли задача (если не постоянная, то сбросится или удалится на следующий день/период)
        public bool IsCompleted { get; set; } = false;
    }
}