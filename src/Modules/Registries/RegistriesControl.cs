using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WorkSpace.Core;
using WorkSpace.Modules.Team;

namespace WorkSpace.Modules.Registries
{
        public partial class RegistriesControl : UserControl, IWorkspaceModule
    {
        private readonly NetworkStorage _storage;
        private readonly AccessService _access;
        private readonly ComboBox _cmbRegistries;
        private readonly TextBox _txtSearch;
        private readonly DataGridView _grid;
        private DateTime _lastLoaded = DateTime.MinValue;
        private RegistriesDatabase _cachedDb;
        private bool _isBindingCombo = false;
        private const string FileName = "registries.json";

        public string Title { get { return "Внутренние реестры"; } }
        public Control View { get { return this; } }

        public RegistriesControl(NetworkStorage storage, AccessService access)
        {
            _storage = storage;
            _access = access;

            Dock = DockStyle.Fill;

            // 1. Верхняя панель с кнопками управления
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                Padding = new Padding(6, 6, 6, 2),
                BackColor = Color.WhiteSmoke,
                WrapContents = false
            };

            _cmbRegistries = new ComboBox { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 4, 6, 0) };
            _cmbRegistries.SelectedIndexChanged += (s, e) => { if (!_isBindingCombo) RenderGrid(); };

            var btnNewReg = new Button { Text = "+ Реестр", Width = 80, Height = 26 };
            btnNewReg.Click += OnCreateRegistryClick;

            var btnEditStruct = new Button { Text = "Структура", Width = 85, Height = 26 };
            btnEditStruct.Click += OnEditStructureClick;

            var btnAddRow = new Button { Text = "+ Запись", Width = 85, Height = 26 };
            btnAddRow.Click += OnAddRowClick;

            var btnEditRow = new Button { Text = "Изменить", Width = 85, Height = 26 };
            btnEditRow.Click += OnEditRowClick;

            var btnDelRow = new Button { Text = "Удалить", Width = 80, Height = 26 };
            btnDelRow.Click += OnDeleteRowClick;

            var btnExport = new Button { Text = "Excel (CSV)", Width = 90, Height = 26 };
            
	    btnExport.Click += OnExportCsvClick;
            btnNewReg.Enabled = _access.IsAdmin();
            btnEditStruct.Enabled = _access.IsAdmin();
            btnAddRow.Enabled = _access.CanEditContent();
            btnEditRow.Enabled = _access.CanEditContent();
            btnDelRow.Enabled = _access.CanEditContent();


            topPanel.Controls.Add(new Label { Text = "Реестр:", AutoSize = true, Margin = new Padding(0, 7, 2, 0) });
            topPanel.Controls.Add(_cmbRegistries);
            topPanel.Controls.Add(btnNewReg);
            topPanel.Controls.Add(btnEditStruct);
            topPanel.Controls.Add(btnAddRow);
            topPanel.Controls.Add(btnEditRow);
            topPanel.Controls.Add(btnDelRow);
            topPanel.Controls.Add(btnExport);

            // 2. Вторая панель (ниже кнопок) — полноширинный поиск
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(8, 4, 8, 6),
                BackColor = Color.WhiteSmoke
            };

            var lblSearch = new Label
            {
                Text = "Поиск по записям:",
                Dock = DockStyle.Left,
                Width = 115,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill
            };
            _txtSearch.TextChanged += (s, e) => RenderGrid();

            var btnClearSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 28
            };
            btnClearSearch.Click += (s, e) => _txtSearch.Clear();

            searchPanel.Controls.Add(_txtSearch);
            searchPanel.Controls.Add(btnClearSearch);
            searchPanel.Controls.Add(lblSearch);

            // 3. Таблица записей
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
            _grid.CellDoubleClick += OnEditRowClick;

            // Порядок добавления важен для DockStyle: сначала Fill, затем нижний Top, затем верхний Top
            Controls.Add(_grid);
            Controls.Add(searchPanel);
            Controls.Add(topPanel);

            Load += (s, e) => RefreshData();
        }


        public void RefreshData()
        {
            DateTime serverTime = _storage.GetLastModified(FileName);
            if (serverTime <= _lastLoaded && _cachedDb != null) return;

            var current = _cmbRegistries.SelectedItem as RegistryDefinition;
            string selectedId = current != null ? current.Id : null;
            _cachedDb = _storage.Read<RegistriesDatabase>(FileName);

            _isBindingCombo = true;
            _cmbRegistries.DataSource = null;
            _cmbRegistries.DisplayMember = "Name";
            _cmbRegistries.ValueMember = "Id";
            _cmbRegistries.DataSource = _cachedDb.Registries;

            if (selectedId != null)
            {
                var found = _cachedDb.Registries.FirstOrDefault(r => r.Id == selectedId);
                if (found != null) _cmbRegistries.SelectedItem = found;
            }
            _isBindingCombo = false;

            RenderGrid();
            _lastLoaded = serverTime;
        }

        private void RenderGrid()
        {
            var reg = _cmbRegistries.SelectedItem as RegistryDefinition;
            if (reg == null)
            {
                _grid.DataSource = null;
                return;
            }

            var table = new DataTable();
            table.Columns.Add("_RowId");
            foreach (var col in reg.Columns)
                table.Columns.Add(col);
            table.Columns.Add("Изменил");

            string filter = _txtSearch.Text.Trim().ToLowerInvariant();

            foreach (var row in reg.Rows)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    bool match = row.Values.Values.Any(v => v != null && v.ToLowerInvariant().Contains(filter));
                    if (!match) continue;
                }

                var dr = table.NewRow();
                dr["_RowId"] = row.Id;
                foreach (var col in reg.Columns)
                    dr[col] = row.Values.ContainsKey(col) ? row.Values[col] : "";
                dr["Изменил"] = string.Format("{0} ({1})", row.UpdatedBy, row.UpdatedAt);
                table.Rows.Add(dr);
            }

            _grid.DataSource = table;
            if (_grid.Columns.Contains("_RowId"))
                _grid.Columns["_RowId"].Visible = false;
        }
    }
}
