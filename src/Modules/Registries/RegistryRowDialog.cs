using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WorkSpace.Modules.Registries
{
    public class RegistryRowDialog : Form
    {
        private readonly Dictionary<string, TextBox> _inputs = new Dictionary<string, TextBox>();
        public Dictionary<string, string> ResultValues { get; private set; }

        public RegistryRowDialog(string title, List<string> columns, Dictionary<string, string> initialValues = null)
        {
            Text = title;
            Size = new Size(480, Math.Min(650, 145 + columns.Count * 38));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = columns.Count + 1,
                Padding = new Padding(14, 12, 14, 6),
                AutoScroll = true
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));

            for (int i = 0; i < columns.Count; i++)
            {
                string col = columns[i];
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

                var lbl = new Label
                {
                    Text = col + ":",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0)
                };

                var txt = new TextBox
                {
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    Margin = new Padding(4, 0, 0, 0),
                    Text = (initialValues != null && initialValues.ContainsKey(col)) ? initialValues[col] : ""
                };

                _inputs[col] = txt;
                panel.Controls.Add(lbl, 0, i);
                panel.Controls.Add(txt, 1, i);
            }

            // Пустая строка-заглушка внизу, чтобы последняя строка не растягивалась и не съезжала
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 6, 12, 6)
            };

            var btnCancel = new Button { Text = "Отмена", Width = 95, Height = 28, DialogResult = DialogResult.Cancel };
            var btnSave = new Button { Text = "Сохранить", Width = 105, Height = 28 };
            btnSave.Click += (s, e) =>
            {
                ResultValues = new Dictionary<string, string>();
                foreach (var kv in _inputs)
                    ResultValues[kv.Key] = kv.Value.Text.Trim();

                DialogResult = DialogResult.OK;
                Close();
            };

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);

            Controls.Add(panel);
            Controls.Add(btnPanel);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}
