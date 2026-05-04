using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Read connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddSingleton<TaskStore>(_ => new TaskStore(connectionString));

var app = builder.Build();

// Create the DB table if it doesn't exist yet
app.Services.GetRequiredService<TaskStore>().InitDb();

app.UseStaticFiles();
app.UseDefaultFiles();

// GET all tasks
app.MapGet("/api/tasks", (TaskStore store) => store.GetAll());

// POST add new task
app.MapPost("/api/tasks", (TaskItem task, TaskStore store) =>
{
    store.Add(task.Title);
    return Results.Ok();
});

// PUT toggle task status
app.MapPut("/api/tasks/{id:int}", (int id, TaskStore store) =>
{
    store.Toggle(id);
    return Results.Ok();
});

// DELETE a task
app.MapDelete("/api/tasks/{id:int}", (int id, TaskStore store) =>
{
    store.Delete(id);
    return Results.Ok();
});

app.Run();

// ── Model ────────────────────────────────────────────────────────────────────

public class TaskItem
{
    public int    Id        { get; set; }
    public string Title     { get; set; } = "";
    public bool   Completed { get; set; }
}

// ── SQL Store (replaces in-memory List) ──────────────────────────────────────

public class TaskStore
{
    private readonly string _connStr;

    public TaskStore(string connectionString)
    {
        _connStr = connectionString;
    }

    // Creates the Tasks table if it doesn't already exist
    public void InitDb()
    {
        using var conn = new SqlConnection(_connStr);
        conn.Execute(@"
            IF NOT EXISTS (
                SELECT * FROM sysobjects WHERE name='Tasks' AND xtype='U'
            )
            CREATE TABLE Tasks (
                Id        INT IDENTITY(1,1) PRIMARY KEY,
                Title     NVARCHAR(200)     NOT NULL,
                Completed BIT               NOT NULL DEFAULT 0
            )
        ");
    }

    public IEnumerable<TaskItem> GetAll()
    {
        using var conn = new SqlConnection(_connStr);
        return conn.Query<TaskItem>("SELECT Id, Title, Completed FROM Tasks ORDER BY Id DESC");
    }

    public void Add(string title)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Execute("INSERT INTO Tasks (Title) VALUES (@Title)", new { Title = title });
    }

    public void Toggle(int id)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Execute(
            "UPDATE Tasks SET Completed = CASE WHEN Completed = 1 THEN 0 ELSE 1 END WHERE Id = @Id",
            new { Id = id }
        );
    }

    public void Delete(int id)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Execute("DELETE FROM Tasks WHERE Id = @Id", new { Id = id });
    }
}
