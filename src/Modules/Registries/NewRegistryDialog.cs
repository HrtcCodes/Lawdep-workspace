using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WorkSpace.Modules.Registries
{
    public class NewRegistryDialog : Form
    {
        public string RegistryName { get; private set; }
        public string[] ColumnsList { get; private set; }

        public NewRegistryDialog()
        {
            Text = "Создать новый реестр";
            Size = new Size(460, 230);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblName = new Label { Text = "Название реестра (например: Учет пропусков):", Left = 15, Top = 15, Width = 410 };
            var txtName = new TextBox { Left = 15, Top = 38, Width = 410 };

            var lblCols = new Label { Text = "Названия колонок через запятую:", Left = 15, Top = 75, Width = 410 };
            var txtCols = new TextBox { Left = 15, Top = 98, Width = 410, Text = "Номер, Наименование, Ответственный, Примечание" };

            var btnCreate = new Button { Text = "Создать", Left = 230, Top = 145, Width = 95, Height = 28 };
            var btnCancel = new Button { Text = "Отмена", Left = 330, Top = 145, Width = 95, Height = 28, DialogResult = DialogResult.Cancel };

            btnCreate.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCols.Text))
                {
                    MessageBox.Show("Заполните название и хотя бы одну колонку.", "Внимание");
                    return;
                }

                RegistryName = txtName.Text.Trim();
                ColumnsList = txtCols.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .Where(c => c.Length > 0)
                                          .Distinct()
                                          .ToArray();

                if (ColumnsList.Length == 0) return;

                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { lblName, txtName, lblCols, txtCols, btnCreate, btnCancel });
            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }
    }
}
