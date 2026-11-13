using System;
using System.Collections.Generic;

namespace WorkSpace.Modules.Tasks
{
    public class TaskItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Assignee { get; set; }
        public string Status { get; set; } // "Новая", "В работе", "Готово"
        public string Author { get; set; }
        public string CreatedAt { get; set; }

        public TaskItem()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            Status = "Новая";
            CreatedAt = DateTime.Now.ToString("dd.MM HH:mm");
        }
    }

    public class TasksDatabase
    {
        public List<TaskItem> Items { get; set; }
        public TasksDatabase() { Items = new List<TaskItem>(); }
    }
}
