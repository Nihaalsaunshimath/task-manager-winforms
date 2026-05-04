using System.Collections.Generic;
using Dapper;
using Microsoft.Data.SqlClient;

namespace TaskManager
{
    public class TaskRepository
    {
        private readonly string _connStr;

        public TaskRepository(string connectionString)
        {
            _connStr = connectionString;
        }

        // Called once at startup — creates table if it doesn't exist
        public void InitDb()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Execute(@"
                IF NOT EXISTS (
                    SELECT * FROM sysobjects WHERE name='Tasks' AND xtype='U'
                )
                CREATE TABLE Tasks (
                    Id        INT IDENTITY(1,1) PRIMARY KEY,
                    Title     NVARCHAR(200) NOT NULL,
                    Completed BIT NOT NULL DEFAULT 0
                )
            ");
        }

        public IEnumerable<TaskItem> GetAll()
        {
            using var conn = new SqlConnection(_connStr);
            return conn.Query<TaskItem>(
                "SELECT Id, Title, Completed FROM Tasks ORDER BY Id DESC");
        }

        public void Add(string title)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Execute("INSERT INTO Tasks (Title) VALUES (@Title)", new { Title = title });
        }

        public void Toggle(int id)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Execute(@"
                UPDATE Tasks
                SET Completed = CASE WHEN Completed = 1 THEN 0 ELSE 1 END
                WHERE Id = @Id", new { Id = id });
        }

        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Execute("DELETE FROM Tasks WHERE Id = @Id", new { Id = id });
        }
    }
}
