namespace TaskManager
{
    public class TaskItem
    {
        public int    Id        { get; set; }
        public string Title     { get; set; } = "";
        public bool   Completed { get; set; }

        public string StatusText => Completed ? "Done" : "Pending";
    }
}
