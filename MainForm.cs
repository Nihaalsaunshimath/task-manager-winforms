using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TaskManager
{
    public class MainForm : Form
    {
        // ── Connection string — update this to match your SQL Server ──────────
        private const string ConnStr =
            "Server=localhost\\SQLEXPRESS;Database=TaskManagerDb;" +
            "Integrated Security=True;TrustServerCertificate=True;";
        // ──────────────────────────────────────────────────────────────────────

        private readonly TaskRepository _repo;

        // Controls
        private Panel       pnlHeader   = new();
        private Label       lblTitle    = new();
        private Label       lblSubtitle = new();
        private Panel       pnlStats    = new();
        private Label       lblTotal    = new();
        private Label       lblPending  = new();
        private Label       lblDone     = new();
        private Panel       pnlAdd      = new();
        private TextBox     txtTask     = new();
        private Button      btnAdd      = new();
        private ListView    lvTasks     = new();
        private Button      btnToggle   = new();
        private Button      btnDelete   = new();
        private StatusStrip statusBar   = new();
        private ToolStripStatusLabel lblStatus = new();

        public MainForm()
        {
            _repo = new TaskRepository(ConnStr);

            try
            {
                _repo.InitDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not connect to SQL Server:\n\n{ex.Message}\n\n" +
                    "Please update the connection string in MainForm.cs",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            BuildUI();
            LoadTasks();
        }

        // ── UI Construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            // Form
            Text            = "Task Manager";
            Size            = new Size(620, 620);
            MinimumSize     = new Size(500, 500);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(240, 244, 248);
            Font            = new Font("Segoe UI", 9.5f);

            // ── Header ──
            pnlHeader.Dock      = DockStyle.Top;
            pnlHeader.Height    = 80;
            pnlHeader.BackColor = Color.FromArgb(45, 106, 79);
            pnlHeader.Padding   = new Padding(20, 10, 20, 10);

            lblTitle.Text      = "✅  Task Manager";
            lblTitle.ForeColor = Color.White;
            lblTitle.Font      = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.AutoSize  = true;
            lblTitle.Location  = new Point(20, 10);

            lblSubtitle.Text      = "Track your daily tasks simply and easily";
            lblSubtitle.ForeColor = Color.FromArgb(200, 255, 220);
            lblSubtitle.Font      = new Font("Segoe UI", 9f);
            lblSubtitle.AutoSize  = true;
            lblSubtitle.Location  = new Point(22, 44);

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // ── Stats Bar ──
            pnlStats.Dock      = DockStyle.Top;
            pnlStats.Height    = 60;
            pnlStats.BackColor = Color.FromArgb(240, 244, 248);
            pnlStats.Padding   = new Padding(20, 8, 20, 8);

            lblTotal   = MakeStatLabel("Total: 0",   Color.FromArgb(45, 106, 79));
            lblPending = MakeStatLabel("Pending: 0", Color.FromArgb(202, 138, 4));
            lblDone    = MakeStatLabel("Done: 0",    Color.FromArgb(22, 101, 52));

            var flowStats = new FlowLayoutPanel
            {
                Dock            = DockStyle.Fill,
                FlowDirection   = FlowDirection.LeftToRight,
                WrapContents    = false,
                BackColor       = Color.Transparent
            };
            flowStats.Controls.AddRange(new Control[] { lblTotal, lblPending, lblDone });
            pnlStats.Controls.Add(flowStats);

            // ── Add Task Row ──
            pnlAdd.Dock        = DockStyle.Top;
            pnlAdd.Height      = 54;
            pnlAdd.BackColor   = Color.White;
            pnlAdd.Padding     = new Padding(20, 10, 20, 10);

            txtTask.Dock        = DockStyle.Fill;
            txtTask.Font        = new Font("Segoe UI", 10f);
            txtTask.PlaceholderText = "Enter a new task...";
            txtTask.BorderStyle = BorderStyle.FixedSingle;
            txtTask.KeyDown    += (s, e) => { if (e.KeyCode == Keys.Enter) AddTask(); };

            btnAdd.Text        = "Add Task";
            btnAdd.Dock        = DockStyle.Right;
            btnAdd.Width       = 100;
            btnAdd.BackColor   = Color.FromArgb(45, 106, 79);
            btnAdd.ForeColor   = Color.White;
            btnAdd.FlatStyle   = FlatStyle.Flat;
            btnAdd.Font        = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnAdd.Cursor      = Cursors.Hand;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click      += (s, e) => AddTask();

            pnlAdd.Controls.Add(txtTask);
            pnlAdd.Controls.Add(btnAdd);

            // ── ListView ──
            lvTasks.Dock         = DockStyle.Fill;
            lvTasks.View         = View.Details;
            lvTasks.FullRowSelect= true;
            lvTasks.GridLines    = true;
            lvTasks.BorderStyle  = BorderStyle.None;
            lvTasks.BackColor    = Color.White;
            lvTasks.Font         = new Font("Segoe UI", 9.5f);
            lvTasks.MultiSelect  = false;
            lvTasks.HeaderStyle  = ColumnHeaderStyle.Nonclickable;

            lvTasks.Columns.Add("#",      40);
            lvTasks.Columns.Add("Task",   350);
            lvTasks.Columns.Add("Status", 100);

            lvTasks.SelectedIndexChanged += (s, e) => UpdateButtonStates();
            lvTasks.DoubleClick          += (s, e) => ToggleTask();

            // ── Action Buttons Row ──
            var pnlActions = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Color.FromArgb(240, 244, 248),
                Padding   = new Padding(20, 10, 20, 10)
            };

            btnToggle.Text      = "Mark as Done";
            btnToggle.Width     = 140;
            btnToggle.Height    = 32;
            btnToggle.Location  = new Point(20, 10);
            btnToggle.BackColor = Color.FromArgb(209, 250, 229);
            btnToggle.ForeColor = Color.FromArgb(6, 95, 70);
            btnToggle.FlatStyle = FlatStyle.Flat;
            btnToggle.Cursor    = Cursors.Hand;
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.Enabled   = false;
            btnToggle.Click    += (s, e) => ToggleTask();

            btnDelete.Text      = "Delete Task";
            btnDelete.Width     = 110;
            btnDelete.Height    = 32;
            btnDelete.Location  = new Point(170, 10);
            btnDelete.BackColor = Color.FromArgb(254, 226, 226);
            btnDelete.ForeColor = Color.FromArgb(153, 27, 27);
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.Cursor    = Cursors.Hand;
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Enabled   = false;
            btnDelete.Click    += (s, e) => DeleteTask();

            pnlActions.Controls.AddRange(new Control[] { btnToggle, btnDelete });

            // ── Status Bar ──
            statusBar.BackColor = Color.FromArgb(45, 106, 79);
            lblStatus.ForeColor = Color.White;
            lblStatus.Text      = "Ready";
            statusBar.Items.Add(lblStatus);

            // ── Assemble ──
            Controls.Add(lvTasks);       // Fill — add first
            Controls.Add(pnlActions);    // Bottom
            Controls.Add(pnlAdd);        // Top (reverse order for DockStyle)
            Controls.Add(pnlStats);
            Controls.Add(pnlHeader);
            Controls.Add(statusBar);
        }

        private static Label MakeStatLabel(string text, Color color)
        {
            return new Label
            {
                Text      = text,
                ForeColor = color,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(140, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin    = new Padding(0, 0, 10, 0)
            };
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void LoadTasks()
        {
            try
            {
                lvTasks.Items.Clear();
                var tasks = _repo.GetAll().ToList();

                foreach (var t in tasks)
                {
                    var item = new ListViewItem(t.Id.ToString());
                    item.SubItems.Add(t.Title);
                    item.SubItems.Add(t.StatusText);
                    item.Tag = t;

                    if (t.Completed)
                    {
                        item.ForeColor = Color.Gray;
                        item.BackColor = Color.FromArgb(236, 253, 245);
                    }

                    lvTasks.Items.Add(item);
                }

                UpdateStats(tasks);
                SetStatus($"Loaded {tasks.Count} task(s).");
            }
            catch (Exception ex)
            {
                SetStatus("DB Error: " + ex.Message);
            }
        }

        private void AddTask()
        {
            var title = txtTask.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a task title.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _repo.Add(title);
                txtTask.Clear();
                txtTask.Focus();
                LoadTasks();
                SetStatus($"Task \"{title}\" added.");
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message);
            }
        }

        private void ToggleTask()
        {
            if (lvTasks.SelectedItems.Count == 0) return;

            var task = (TaskItem)lvTasks.SelectedItems[0].Tag!;
            try
            {
                _repo.Toggle(task.Id);
                LoadTasks();
                SetStatus($"Task \"{task.Title}\" marked as {(task.Completed ? "Pending" : "Done")}.");
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message);
            }
        }

        private void DeleteTask()
        {
            if (lvTasks.SelectedItems.Count == 0) return;

            var task = (TaskItem)lvTasks.SelectedItems[0].Tag!;
            var confirm = MessageBox.Show(
                $"Delete task:\n\"{task.Title}\"?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _repo.Delete(task.Id);
                LoadTasks();
                SetStatus($"Task \"{task.Title}\" deleted.");
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message);
            }
        }

        private void UpdateButtonStates()
        {
            bool selected = lvTasks.SelectedItems.Count > 0;
            btnToggle.Enabled = selected;
            btnDelete.Enabled = selected;

            if (selected)
            {
                var task = (TaskItem)lvTasks.SelectedItems[0].Tag!;
                btnToggle.Text      = task.Completed ? "Mark as Pending" : "Mark as Done";
                btnToggle.BackColor = task.Completed
                    ? Color.FromArgb(254, 243, 199)
                    : Color.FromArgb(209, 250, 229);
                btnToggle.ForeColor = task.Completed
                    ? Color.FromArgb(146, 64, 14)
                    : Color.FromArgb(6, 95, 70);
            }
        }

        private void UpdateStats(List<TaskItem> tasks)
        {
            int total   = tasks.Count;
            int done    = tasks.Count(t => t.Completed);
            int pending = total - done;

            lblTotal.Text   = $"Total: {total}";
            lblPending.Text = $"Pending: {pending}";
            lblDone.Text    = $"Done: {done}";
        }

        private void SetStatus(string msg)
        {
            lblStatus.Text = msg;
        }
    }
}
