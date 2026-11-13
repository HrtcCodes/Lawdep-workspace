using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WorkSpace.Modules.Tasks
{
    public class TaskEditDialog : Form
    {
        public string UpdatedTitle { get; private set; }
        public string UpdatedAssignee { get; private set; }
        public string UpdatedStatus { get; private set; }
        public bool IsDeleted { get; private set; }

        private readonly TextBox _txtTitle;
        private readonly ComboBox _cmbAssignee;
        private readonly ComboBox _cmbStatus;

        public TaskEditDialog(TaskItem task, List<string> assignees, bool canEdit, bool canDelete)
        {
            Text = string.Format("Задача [{0}] — от {1} ({2})", task.Id, task.Author, task.CreatedAt);
            Size = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label { Text = "Описание задачи:", Left = 15, Top = 14, Width = 435 };
            _txtTitle = new TextBox
            {
                Left = 15,
                Top = 36,
                Width = 435,
                Height = 95,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = task.Title,
                ReadOnly = !canEdit
            };

            var lblAssignee = new Label { Text = "Исполнитель:", Left = 15, Top = 145, Width = 210 };
            _cmbAssignee = new ComboBox
            {
                Left = 15,
                Top = 167,
                Width = 225,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = canEdit
            };

            foreach (var a in assignees)
                _cmbAssignee.Items.Add(a);

            if (!string.IsNullOrEmpty(task.Assignee) && !_cmbAssignee.Items.Contains(task.Assignee))
                _cmbAssignee.Items.Add(task.Assignee);

            _cmbAssignee.SelectedItem = task.Assignee;

            var lblStatus = new Label { Text = "Статус:", Left = 255, Top = 145, Width = 195 };
            _cmbStatus = new ComboBox
            {
                Left = 255,
                Top = 167,
                Width = 195,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = canEdit
            };
            _cmbStatus.Items.AddRange(new object[] { "Новая", "В работе", "Готово" });
            _cmbStatus.SelectedItem = task.Status;

            // Кнопка удаления задачи (доступна автору задачи или Администратору)
            var btnDelete = new Button
            {
                Text = "Удалить задачу",
                Left = 15,
                Top = 225,
                Width = 125,
                Height = 30,
                ForeColor = Color.DarkRed,
                Visible = canDelete
            };
            btnDelete.Click += (s, e) =>
            {
                var res = MessageBox.Show(
                    "Удалить эту задачу безвозвратно?",
                    "Удаление задачи",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (res == DialogResult.Yes)
                {
                    IsDeleted = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            var btnSave = new Button
            {
                Text = "Сохранить",
                Left = 250,
                Top = 225,
                Width = 95,
                Height = 30,
                Enabled = canEdit
            };
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtTitle.Text))
                {
                    MessageBox.Show("Описание задачи не может быть пустым.", "Внимание");
                    return;
                }

                UpdatedTitle = _txtTitle.Text.Trim();
                UpdatedAssignee = _cmbAssignee.SelectedItem != null ? _cmbAssignee.SelectedItem.ToString() : "Не назначен";
                UpdatedStatus = _cmbStatus.SelectedItem != null ? _cmbStatus.SelectedItem.ToString() : "Новая";
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnCancel = new Button
            {
                Text = "Закрыть",
                Left = 355,
                Top = 225,
                Width = 95,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[]
            {
                lblTitle, _txtTitle,
                lblAssignee, _cmbAssignee,
                lblStatus, _cmbStatus,
                btnDelete, btnSave, btnCancel
            });
            CancelButton = btnCancel;
        }
    }
}
