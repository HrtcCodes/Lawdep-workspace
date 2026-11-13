using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WorkSpace.Core;

namespace WorkSpace.Modules.Team
{
    public class TeamControl : UserControl, IWorkspaceModule
    {
        private readonly NetworkStorage _storage;
        private readonly AccessService _access;
        private readonly DataGridView _grid;
        private readonly TextBox _txtLogin;
        private readonly TextBox _txtFullName;
        private readonly ComboBox _cmbRole;
        private readonly FlowLayoutPanel _adminPanel;
        private DateTime _lastLoaded = DateTime.MinValue;
        private const string FileName = "team.json";

        public string Title { get { return "Команда и доступ"; } }
        public Control View { get { return this; } }

        public TeamControl(NetworkStorage storage, AccessService access)
        {
            _storage = storage;
            _access = access;
            Dock = DockStyle.Fill;

            // Информационная плашка с текущей ролью пользователя
            var infoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(10, 6, 10, 4),
                BackColor = Color.AliceBlue
            };

            var lblMyRole = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = string.Format("Ваш логин: {0}   |   Ваша роль: {1}", AppConfig.CurrentUser, _access.GetCurrentRole())
            };
            infoPanel.Controls.Add(lblMyRole);

            // Панель управления (видна и доступна ТОЛЬКО Администраторам)
            bool isAdmin = _access.IsAdmin();
            _adminPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(8, 8, 8, 4),
                BackColor = Color.WhiteSmoke,
                WrapContents = false,
                Visible = isAdmin,
                Enabled = isAdmin
            };

            _txtLogin = new TextBox { Width = 130, Margin = new Padding(3, 4, 8, 0) };
            _txtFullName = new TextBox { Width = 180, Margin = new Padding(3, 4, 8, 0) };
            _cmbRole = new ComboBox { Width = 135, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 4, 8, 0) };
            _cmbRole.Items.AddRange(new object[] { Roles.Admin, Roles.Editor, Roles.Viewer });
            _cmbRole.SelectedIndex = 1; // По умолчанию "Редактор"

            var btnAdd = new Button { Text = "+ Добавить / Обновить", Width = 145, Height = 26 };
            btnAdd.Click += OnSaveMemberClick;

            var btnRemove = new Button { Text = "Удалить", Width = 80, Height = 26 };
            btnRemove.Click += OnRemoveMemberClick;

            _adminPanel.Controls.Add(new Label { Text = "Логин Windows:", AutoSize = true, Margin = new Padding(0, 7, 0, 0) });
            _adminPanel.Controls.Add(_txtLogin);
            _adminPanel.Controls.Add(new Label { Text = "ФИО / Отдел:", AutoSize = true, Margin = new Padding(0, 7, 0, 0) });
            _adminPanel.Controls.Add(_txtFullName);
            _adminPanel.Controls.Add(new Label { Text = "Роль:", AutoSize = true, Margin = new Padding(0, 7, 0, 0) });
            _adminPanel.Controls.Add(_cmbRole);
            _adminPanel.Controls.Add(btnAdd);
            _adminPanel.Controls.Add(btnRemove);

            // Таблица команды
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

            _grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in _grid.Columns)
                {
                    if (col.Name == "Login") { col.HeaderText = "Логин Windows"; col.FillWeight = 80; }
                    else if (col.Name == "FullName") { col.HeaderText = "ФИО / Должность"; col.FillWeight = 140; }
                    else if (col.Name == "Role") { col.HeaderText = "Роль доступа"; col.FillWeight = 80; }
                    else if (col.Name == "AddedAt") { col.HeaderText = "Добавлен"; col.FillWeight = 60; }
                }
            };

            // Клик по строке заполняет поля сверху только для Администратора
            _grid.SelectionChanged += (s, e) =>
            {
                if (!_access.IsAdmin() || _grid.CurrentRow == null) return;
                var member = _grid.CurrentRow.DataBoundItem as TeamMember;
                if (member != null)
                {
                    _txtLogin.Text = member.Login;
                    _txtFullName.Text = member.FullName;
                    _cmbRole.SelectedItem = member.Role;
                }
            };

            Controls.Add(_grid);
            Controls.Add(_adminPanel);
            Controls.Add(infoPanel);

            Load += (s, e) => RefreshData();
        }

                public void RefreshData()
        {
            DateTime serverTime = _storage.GetLastModified(FileName);
            if (serverTime <= _lastLoaded && _grid.DataSource != null) return;

            _access.RefreshTeamKey();

            _grid.DataSource = null;
            _grid.DataSource = _access.GetAllMembers();
            _lastLoaded = serverTime;
        }

        private void OnSaveMemberClick(object sender, EventArgs e)
        {
            if (!_access.IsAdmin())
            {
                MessageBox.Show("Недостаточно прав для изменения состава команды или ролей.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string login = _txtLogin.Text.Trim();
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин пользователя Windows (например: ivanov_ii).", "Внимание");
                return;
            }

            string role = _cmbRole.SelectedItem.ToString();

            if (login.Equals(AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase) && role != Roles.Admin)
            {
                MessageBox.Show(
                    "Вы не можете понизить собственную роль ниже «Администратор».",
                    "Защита роли",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _cmbRole.SelectedItem = Roles.Admin;
                return;
            }

            string fullName = string.IsNullOrWhiteSpace(_txtFullName.Text) ? login : _txtFullName.Text.Trim();

            var members = _access.GetAllMembers();
            var tasksDb = _storage.Read<WorkSpace.Modules.Tasks.TasksDatabase>("tasks.json");
            var registriesDb = _storage.Read<WorkSpace.Modules.Registries.RegistriesDatabase>("registries.json");

            var existing = members.FirstOrDefault(m => m.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.FullName = fullName;
                existing.Role = role;
            }
            else
            {
                members.Add(new TeamMember { Login = login, FullName = fullName, Role = role });
            }

            _access.SaveTeamMembers(members);

            // 4. Перешифровываем остальные базы новым ключом напрямую
            _storage.Overwrite("tasks.json", tasksDb);
            _storage.Overwrite("registries.json", registriesDb);

            _txtLogin.Clear();
            _txtFullName.Clear();
            _lastLoaded = DateTime.MinValue;
            RefreshData();
        }

        private void OnRemoveMemberClick(object sender, EventArgs e)
        {
            if (!_access.IsAdmin()) return;
            if (_grid.CurrentRow == null) return;

            var selected = _grid.CurrentRow.DataBoundItem as TeamMember;
            if (selected == null) return;

            if (selected.Login.Equals(AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Нельзя удалить самого себя из команды.", "Защита");
                return;
            }

            if (MessageBox.Show("Удалить сотрудника «" + selected.FullName + "» из команды и перешифровать базы?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            var members = _access.GetAllMembers();
            var tasksDb = _storage.Read<WorkSpace.Modules.Tasks.TasksDatabase>("tasks.json");
            var registriesDb = _storage.Read<WorkSpace.Modules.Registries.RegistriesDatabase>("registries.json");

            members.RemoveAll(m => m.Login.Equals(selected.Login, StringComparison.OrdinalIgnoreCase));

            _access.SaveTeamMembers(members);

            // 4. Перешифровываем остальные базы новым ключом напрямую
            _storage.Overwrite("tasks.json", tasksDb);
            _storage.Overwrite("registries.json", registriesDb);

            _lastLoaded = DateTime.MinValue;
            RefreshData();
        }
    }
}
