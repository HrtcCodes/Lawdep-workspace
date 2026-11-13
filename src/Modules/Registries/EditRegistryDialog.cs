using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WorkSpace.Modules.Registries
{
    public class EditRegistryDialog : Form
    {
        public string UpdatedName { get; private set; }
        public List<string> UpdatedColumns { get; private set; }
        public Dictionary<string, string> RenamedMap { get; private set; }
        public bool IsDeleted { get; private set; }

        private readonly RegistryDefinition _registry;
        private readonly DataGridView _gridCols;
        private readonly TextBox _txtName;
        private readonly Label _lblDirtyWarning;

        public EditRegistryDialog(RegistryDefinition registry)
        {
            _registry = registry;
            Text = "Настройка структуры реестра";
            Size = new Size(510, 475);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            RenamedMap = new Dictionary<string, string>();

            var lblName = new Label { Text = "Название реестра:", Left = 15, Top = 14, Width = 460 };
            _txtName = new TextBox { Left = 15, Top = 36, Width = 460, Text = registry.Name };
            _txtName.TextChanged += (s, e) => MarkDirty();

            var lblHint = new Label
            {
                Text = "Колонки (дважды кликните для переименования):",
                Left = 15,
                Top = 70,
                Width = 460
            };

            _gridCols = new DataGridView
            {
                Left = 15,
                Top = 92,
                Width = 355,
                Height = 240,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            _gridCols.Columns.Add("OldName", "Исходное");
            _gridCols.Columns["OldName"].Visible = false;
            _gridCols.Columns.Add("NewName", "Название колонки");

            // Отключаем сортировку при клике на заголовок колонки
            foreach (DataGridViewColumn c in _gridCols.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            foreach (var col in registry.Columns)
            {
                _gridCols.Rows.Add(col, col);
            }

            _gridCols.CellValueChanged += (s, e) => MarkDirty();

            // Кнопки управления колонками справа
            var btnAddCol = new Button { Text = "+ Колонка", Left = 380, Top = 92, Width = 95, Height = 28 };
            btnAddCol.Click += (s, e) =>
            {
                int idx = _gridCols.Rows.Add("", "Новая колонка " + (_gridCols.Rows.Count + 1));
                _gridCols.CurrentCell = _gridCols.Rows[idx].Cells["NewName"];
                MarkDirty();
                _gridCols.BeginEdit(true);
            };

            var btnDelCol = new Button { Text = "- Удалить", Left = 380, Top = 126, Width = 95, Height = 28 };
            btnDelCol.Click += OnDeleteColumnClick;

            var btnUp = new Button { Text = "Вверх ↑", Left = 380, Top = 170, Width = 95, Height = 28 };
            btnUp.Click += (s, e) => MoveRow(-1);

            var btnDown = new Button { Text = "Вниз ↓", Left = 380, Top = 204, Width = 95, Height = 28 };
            btnDown.Click += (s, e) => MoveRow(1);

            // Предупреждение о несохраненных изменениях снизу
            _lblDirtyWarning = new Label
            {
                Text = "Есть несохраненные изменения. Нажмите «Сохранить» или «Отмена».",
                Left = 15,
                Top = 344,
                Width = 460,
                Height = 22,
                ForeColor = Color.DarkOrange,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Visible = false
            };

            // Нижний ряд кнопок
            var btnDeleteReg = new Button
            {
                Text = "Удалить реестр",
                Left = 15,
                Top = 380,
                Width = 125,
                Height = 30,
                ForeColor = Color.DarkRed
            };
            btnDeleteReg.Click += (s, e) =>
            {
                var res = MessageBox.Show(
                    "Вы уверены, что хотите удалить этот реестр и ВСЕ его записи безвозвратно?",
                    "Удаление реестра",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (res == DialogResult.Yes)
                {
                    IsDeleted = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            var btnSave = new Button { Text = "Сохранить", Left = 275, Top = 380, Width = 95, Height = 30 };
            btnSave.Click += OnSaveClick;

            var btnCancel = new Button { Text = "Отмена", Left = 380, Top = 380, Width = 95, Height = 30, DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[]
            {
                lblName, _txtName, lblHint, _gridCols,
                btnAddCol, btnDelCol, btnUp, btnDown,
                _lblDirtyWarning, btnDeleteReg, btnSave, btnCancel
            });
            CancelButton = btnCancel;
        }

        private void MarkDirty()
        {
            if (!_lblDirtyWarning.Visible)
                _lblDirtyWarning.Visible = true;
        }

        private void OnDeleteColumnClick(object sender, EventArgs e)
        {
            if (_gridCols.CurrentRow == null) return;
            if (_gridCols.Rows.Count <= 1)
            {
                MessageBox.Show("В реестре должна остаться хотя бы одна колонка.", "Внимание");
                return;
            }

            string oldName = (_gridCols.CurrentRow.Cells["OldName"].Value ?? "").ToString();
            string displayName = (_gridCols.CurrentRow.Cells["NewName"].Value ?? "").ToString();

            // Проверяем, есть ли в этой колонке заполненные данные у существующих записей
            if (!string.IsNullOrEmpty(oldName) && _registry.Rows != null)
            {
                int filledCount = _registry.Rows.Count(r =>
                    r.Values != null &&
                    r.Values.ContainsKey(oldName) &&
                    !string.IsNullOrWhiteSpace(r.Values[oldName]));

                if (filledCount > 0)
                {
                    string msg = string.Format(
                        "В колонке «{0}» уже заполнены данные в {1} записях!\nПри сохранении эти значения будут удалены. Удалить колонку?",
                        displayName, filledCount);

                    if (MessageBox.Show(msg, "Внимание: потеря данных", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                }
            }

            _gridCols.Rows.Remove(_gridCols.CurrentRow);
            MarkDirty();
        }

        private void MoveRow(int direction)
        {
            if (_gridCols.CurrentRow == null) return;
            int idx = _gridCols.CurrentRow.Index;
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= _gridCols.Rows.Count) return;

            var row = _gridCols.Rows[idx];
            object oldVal = row.Cells["OldName"].Value;
            object newVal = row.Cells["NewName"].Value;

            _gridCols.Rows.RemoveAt(idx);
            _gridCols.Rows.Insert(newIdx, oldVal, newVal);
            _gridCols.CurrentCell = _gridCols.Rows[newIdx].Cells["NewName"];
            MarkDirty();
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            _gridCols.EndEdit();

            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Введите название реестра.", "Внимание");
                return;
            }

            var cols = new List<string>();
            var map = new Dictionary<string, string>();

            foreach (DataGridViewRow row in _gridCols.Rows)
            {
                string oldName = (row.Cells["OldName"].Value ?? "").ToString();
                string newName = (row.Cells["NewName"].Value ?? "").ToString().Trim();

                if (string.IsNullOrEmpty(newName)) continue;
                if (cols.Contains(newName))
                {
                    MessageBox.Show("Названия колонок не должны повторяться: " + newName, "Ошибка");
                    return;
                }

                cols.Add(newName);
                if (!string.IsNullOrEmpty(oldName))
                {
                    map[oldName] = newName;
                }
            }

            if (cols.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну колонку.", "Внимание");
                return;
            }

            UpdatedName = _txtName.Text.Trim();
            UpdatedColumns = cols;
            RenamedMap = map;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
