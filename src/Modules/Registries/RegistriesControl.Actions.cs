using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WorkSpace.Core;

namespace WorkSpace.Modules.Registries
{
    public partial class RegistriesControl
    {
        private void OnCreateRegistryClick(object sender, EventArgs e)
        {
            using (var dlg = new NewRegistryDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var newReg = new RegistryDefinition
                {
                    Name = dlg.RegistryName,
                    Columns = dlg.ColumnsList.ToList()
                };

                _storage.Update<RegistriesDatabase>(FileName, db => db.Registries.Add(newReg));
                _lastLoaded = DateTime.MinValue;
                RefreshData();
                _cmbRegistries.SelectedItem = _cachedDb.Registries.LastOrDefault();
            }
        }
        
	private void OnEditStructureClick(object sender, EventArgs e)
        {
            var reg = _cmbRegistries.SelectedItem as RegistryDefinition;
            if (reg == null) return;

            using (var dlg = new EditRegistryDialog(reg))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                _storage.Update<RegistriesDatabase>(FileName, db =>
                {
                    var targetReg = db.Registries.FirstOrDefault(r => r.Id == reg.Id);
                    if (targetReg == null) return;

                    if (dlg.IsDeleted)
                    {
                        db.Registries.Remove(targetReg);
                        return;
                    }

                    targetReg.Name = dlg.UpdatedName;
                    targetReg.Columns = dlg.UpdatedColumns;

                    // Аккуратно переносим значения всех существующих строк под новые имена колонок
                    foreach (var row in targetReg.Rows)
                    {
                        var newValues = new Dictionary<string, string>();
                        foreach (var newCol in dlg.UpdatedColumns)
                        {
                            // Ищем, как эта колонка называлась раньше
                            var oldPair = dlg.RenamedMap.FirstOrDefault(kv => kv.Value == newCol);
                            string oldKey = oldPair.Key;

                            if (!string.IsNullOrEmpty(oldKey) && row.Values.ContainsKey(oldKey))
                                newValues[newCol] = row.Values[oldKey];
                            else
                                newValues[newCol] = "";
                        }
                        row.Values = newValues;
                    }
                });

                _lastLoaded = DateTime.MinValue;
                RefreshData();
            }
        }

        private void OnAddRowClick(object sender, EventArgs e)
        {
            var reg = _cmbRegistries.SelectedItem as RegistryDefinition;
            if (reg == null) return;

            using (var dlg = new RegistryRowDialog("Новая запись: " + reg.Name, reg.Columns))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var newRow = new RegistryRow
                {
                    Values = dlg.ResultValues,
                    UpdatedBy = AppConfig.CurrentUser,
                    UpdatedAt = DateTime.Now.ToString("dd.MM HH:mm")
                };

                _storage.Update<RegistriesDatabase>(FileName, db =>
                {
                    var targetReg = db.Registries.FirstOrDefault(r => r.Id == reg.Id);
                    if (targetReg != null) targetReg.Rows.Insert(0, newRow);
                });

                _lastLoaded = DateTime.MinValue;
                RefreshData();
            }
        }

                private void OnEditRowClick(object sender, EventArgs e)
        {
            if (!_access.CanEditContent()) return;
            var reg = _cmbRegistries.SelectedItem as RegistryDefinition;
            if (reg == null || _grid.CurrentRow == null) return;

            string rowId = _grid.CurrentRow.Cells["_RowId"].Value.ToString();
            var existingRow = reg.Rows.FirstOrDefault(r => r.Id == rowId);
            if (existingRow == null) return;

            using (var dlg = new RegistryRowDialog("Редактирование записи", reg.Columns, existingRow.Values))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                _storage.Update<RegistriesDatabase>(FileName, db =>
                {
                    var targetReg = db.Registries.FirstOrDefault(r => r.Id == reg.Id);
                    if (targetReg == null) return;
                    var targetRow = targetReg.Rows.FirstOrDefault(r => r.Id == rowId);
                    if (targetRow != null)
                    {
                        targetRow.Values = dlg.ResultValues;
                        targetRow.UpdatedBy = AppConfig.CurrentUser;
                        targetRow.UpdatedAt = DateTime.Now.ToString("dd.MM HH:mm");
                    }
                });

                _lastLoaded = DateTime.MinValue;
                RefreshData();
            }
        }

        private void OnDeleteRowClick(object sender, EventArgs e)
        {
            var reg = _cmbRegistries.SelectedItem as RegistryDefinition;
            if (reg == null || _grid.CurrentRow == null) return;

            if (MessageBox.Show("Удалить выбранную запись из реестра?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            string rowId = _grid.CurrentRow.Cells["_RowId"].Value.ToString();

            _storage.Update<RegistriesDatabase>(FileName, db =>
            {
                var targetReg = db.Registries.FirstOrDefault(r => r.Id == reg.Id);
                if (targetReg != null) targetReg.Rows.RemoveAll(r => r.Id == rowId);
            });

            _lastLoaded = DateTime.MinValue;
            RefreshData();
        }

        private void OnExportCsvClick(object sender, EventArgs e)
        {
            var reg = _cmbRegistries.SelectedItem as RegistryDefinition;
            if (reg == null) return;

            using (var sfd = new SaveFileDialog { Filter = "CSV файл|*.csv", FileName = reg.Name + ".csv" })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                char q = (char)34;
                string quote = q.ToString();
                string doubleQuote = quote + quote;

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(";", reg.Columns));

                foreach (var row in reg.Rows)
                {
                    var line = reg.Columns.Select(c =>
                    {
                        string val = row.Values.ContainsKey(c) ? row.Values[c] : "";
                        return quote + val.Replace(quote, doubleQuote) + quote;
                    });
                    sb.AppendLine(string.Join(";", line));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show("Реестр успешно выгружен!", "Экспорт");
            }
        }
    }
}
