using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WorkSpace.Core;
using WorkSpace.Modules.Team;

namespace WorkSpace.Modules.Tasks
{
        public class TasksControl : UserControl, IWorkspaceModule
    {
        private readonly NetworkStorage _storage;
        private readonly AccessService _access;
        private readonly DataGridView _grid;
        private readonly TextBox _txtTitle;
        private readonly ComboBox _cmbAssignee;
        private TasksDatabase _cachedDb;
        private CheckBox _chkOnlyMine;
        private CheckBox _chkHideDone;
        private Label _lblCount;
        private DateTime _lastLoaded = DateTime.MinValue;
        private DateTime _lastTeamLoaded = DateTime.MinValue;
        private const string FileName = "tasks.json";

        public string Title { get { return "Трекер задач"; } }
        public Control View { get { return this; } }

        public TasksControl(NetworkStorage storage, AccessService access)
        {
            _storage = storage;
            _access = access;

            Dock = DockStyle.Fill;

            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(8),
                BackColor = Color.WhiteSmoke
            };

            _txtTitle = new TextBox { Width = 260 };
            _cmbAssignee = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnAdd = new Button { Text = "+ Добавить задачу", Width = 130, Height = 25 };
            var btnNextStatus = new Button { Text = "Сменить статус ->", Width = 130, Height = 25 };
            btnAdd.Enabled = _access.CanEditContent();
            btnNextStatus.Enabled = _access.CanEditContent();


            btnAdd.Click += OnAddClick;
            btnNextStatus.Click += OnNextStatusClick;

            topPanel.Controls.Add(new Label { Text = "Задача:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) });
            topPanel.Controls.Add(_txtTitle);
            topPanel.Controls.Add(new Label { Text = "Исполнитель:", AutoSize = true, Margin = new Padding(10, 5, 0, 0) });
            topPanel.Controls.Add(_cmbAssignee);
            topPanel.Controls.Add(btnAdd);
            topPanel.Controls.Add(btnNextStatus);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };

            // Безопасная настройка колонок ПОСЛЕ того, как таблица полностью привязала данные и создала окно

            _grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in _grid.Columns)
                {
                    if (col.Name == "Id") { col.HeaderText = "Код"; col.FillWeight = 25; }
                    else if (col.Name == "Title") { col.HeaderText = "Задача"; col.FillWeight = 150; }
                    else if (col.Name == "Assignee") { col.HeaderText = "Исполнитель"; col.FillWeight = 60; }
                    else if (col.Name == "Status") { col.HeaderText = "Статус"; col.FillWeight = 45; }
                    else if (col.Name == "Author") { col.HeaderText = "Автор"; col.FillWeight = 60; }
                    else if (col.Name == "CreatedAt") { col.HeaderText = "Создана"; col.FillWeight = 50; }
                }
            };

// Панель фильтров под строкой создания задачи

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(8, 5, 8, 2),
                BackColor = Color.AliceBlue,
                WrapContents = false
            };

            _chkOnlyMine = new CheckBox
            {
                Text = "Только мои задачи",
                AutoSize = true,
                Margin = new Padding(0, 2, 16, 0)
            };
            _chkOnlyMine.CheckedChanged += (s, e) => ApplyFilters();

            _chkHideDone = new CheckBox
            {
                Text = "Скрыть готовые",
                AutoSize = true,
                Checked = true, // По умолчанию готовые задачи скрыты
                Margin = new Padding(0, 2, 20, 0)
            };
            _chkHideDone.CheckedChanged += (s, e) => ApplyFilters();

            _lblCount = new Label
            {
                Text = "Показано задач: 0",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 3, 0, 0)
            };

            filterPanel.Controls.Add(_chkOnlyMine);
            filterPanel.Controls.Add(_chkHideDone);
            filterPanel.Controls.Add(_lblCount);

            // Порядок добавления важен для DockStyle: Fill -> нижний Top -> верхний Top

            _grid.CellDoubleClick += OnTaskDoubleClick;
            Controls.Add(_grid);
            Controls.Add(filterPanel);
            Controls.Add(topPanel);

            // Загружаем данные только когда вкладка реально отобразится на экране

            Load += (s, e) => RefreshData();
        }

        public void RefreshData()
        {
            RefreshAssigneesList();

            DateTime serverTime = _storage.GetLastModified(FileName);
            if (serverTime <= _lastLoaded && _cachedDb != null) return;

            _cachedDb = _storage.Read<TasksDatabase>(FileName);
            _lastLoaded = serverTime;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_cachedDb == null || _cachedDb.Items == null) return;

            var query = _cachedDb.Items.AsEnumerable();

            if (_chkOnlyMine != null && _chkOnlyMine.Checked)
            {
                query = query.Where(t =>
                    t.Assignee != null &&
                    t.Assignee.IndexOf(AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (_chkHideDone != null && _chkHideDone.Checked)
            {
                query = query.Where(t => t.Status != "Готово");
            }

            var filteredList = query.ToList();

            _grid.DataSource = null;
            _grid.DataSource = filteredList;

            if (_lblCount != null)
            {
                _lblCount.Text = string.Format("Показано задач: {0} из {1}", filteredList.Count, _cachedDb.Items.Count);
            }
        }

        private void RefreshAssigneesList()
        {
            DateTime teamTime = _storage.GetLastModified("team.json");
            if (teamTime <= _lastTeamLoaded && _cmbAssignee.Items.Count > 0) return;

            string prevSelected = _cmbAssignee.SelectedItem != null ? _cmbAssignee.SelectedItem.ToString() : null;
            var members = _access.GetAllMembers();

            _cmbAssignee.Items.Clear();
            _cmbAssignee.Items.Add("Не назначен");

            foreach (var m in members)
            {
                string display = (m.FullName == m.Login)
                    ? m.Login
                    : string.Format("{0} ({1})", m.FullName, m.Login);
                _cmbAssignee.Items.Add(display);
            }

            if (prevSelected != null && _cmbAssignee.Items.Contains(prevSelected))
            {
                _cmbAssignee.SelectedItem = prevSelected;
            }
            else
            {
                // По умолчанию выбираем текущего пользователя
                string me = _cmbAssignee.Items.Cast<string>()
                    .FirstOrDefault(x => x.Contains(AppConfig.CurrentUser));
                _cmbAssignee.SelectedItem = me ?? "Не назначен";
            }

            _lastTeamLoaded = teamTime;
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text)) return;

            var newTask = new TaskItem
            {
                Title = _txtTitle.Text.Trim(),
                Assignee = _cmbAssignee.SelectedItem != null ? _cmbAssignee.SelectedItem.ToString() : "Не назначен",
                Author = AppConfig.CurrentUser
            };

              _storage.Update<TasksDatabase>(FileName, db =>
            {
                // Гарантируем 100% уникальность кода перед сохранением
                while (db.Items.Any(x => x.Id == newTask.Id))
                {
                    newTask.Id = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                }

                db.Items.Insert(0, newTask);
            });
            _txtTitle.Clear();
            _lastLoaded = DateTime.MinValue;
            RefreshData();
        }

        private void OnNextStatusClick(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null) return;
            var selected = _grid.CurrentRow.DataBoundItem as TaskItem;
            if (selected == null) return;

            _storage.Update<TasksDatabase>(FileName, db =>
            {
                var item = db.Items.FirstOrDefault(x => x.Id == selected.Id);
                if (item == null) return;
                if (item.Status == "Новая") item.Status = "В работе";
                else if (item.Status == "В работе") item.Status = "Готово";
                else item.Status = "Новая";
            });

            _lastLoaded = DateTime.MinValue;
            RefreshData();
        }
        private void OnTaskDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.CurrentRow == null) return;
            var selected = _grid.CurrentRow.DataBoundItem as TaskItem;
            if (selected == null) return;

            var assigneesList = _cmbAssignee.Items.Cast<string>().ToList();
            bool canEdit = _access.CanEditContent();
            // Удалять задачу может Администратор или сам автор задачи (если он не Наблюдатель)
            bool canDelete = _access.IsAdmin() ||
                             (canEdit && selected.Author != null && selected.Author.Equals(AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase));

            using (var dlg = new TaskEditDialog(selected, assigneesList, canEdit, canDelete))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                _storage.Update<TasksDatabase>(FileName, db =>
                {
                    var item = db.Items.FirstOrDefault(x => x.Id == selected.Id);
                    if (item == null) return;

                    if (dlg.IsDeleted)
                    {
                        db.Items.Remove(item);
                    }
                    else
                    {
                        item.Title = dlg.UpdatedTitle;
                        item.Assignee = dlg.UpdatedAssignee;
                        item.Status = dlg.UpdatedStatus;
                    }
                });

                _lastLoaded = DateTime.MinValue;
                RefreshData();
            }
        }
    }
}
