using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WorkSpace.Core;
using WorkSpace.Modules.Registries;
using WorkSpace.Modules.Tasks;
using WorkSpace.Modules.Team;
using WorkSpace.UI;

namespace WorkSpace
{
    static class Program
    {
        private const string MutexName = "Local\\WorkSpaceEmployeeAppMutex";
        private const string ShowEventName = "Local\\WorkSpaceShowWindowEvent";

        [STAThread]
        static void Main(string[] args)
        {
            Application.ThreadException += (s, e) => ShowCopyableError(e.Exception);

            try
            {
                string currentExe = Assembly.GetExecutingAssembly().Location;
                string currentDir = Path.GetDirectoryName(currentExe);
                string tempDir = Path.Combine(Path.GetTempPath(), "WorkSpaceApp");

                bool isLocal = (args != null && args.Length > 0 && args.GetValue(0).ToString() == "--local");

                if (!isLocal && !currentDir.Equals(tempDir, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (!Directory.Exists(tempDir))
                            Directory.CreateDirectory(tempDir);

                        string localExe = Path.Combine(tempDir, Path.GetFileName(currentExe));
                        File.Copy(currentExe, localExe, true);

                        string startArgs = string.Format("--local \"{0}\"", currentDir);
                        Process.Start(localExe, startArgs);
                        return;
                    }
                    catch { }
                }

                bool createdNew;
                using (var mutex = new Mutex(true, MutexName, out createdNew))
                {
                    if (!createdNew)
                    {
                        // Программа уже запущена — отправляем сигнал показать окно и тихо выходим
                        try
                        {
                            using (var showEvent = EventWaitHandle.OpenExisting(ShowEventName))
                            {
                                showEvent.Set();
                            }
                        }
                        catch { }
                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    AppConfig.Load(args);
                    var storage = new NetworkStorage();
                    var access = new AccessService(storage);

                    if (access.GetCurrentRole() == "Нет доступа")
                    {
                        MessageBox.Show(
                            "У вашей учетной записи (" + AppConfig.CurrentUser + ") нет доступа к рабочему месту.\nОбратитесь к администратору.",
                            "Доступ ограничен",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    var modules = new List<IWorkspaceModule>
                    {
                        new TasksControl(storage, access),
                        new RegistriesControl(storage, access),
                        new TeamControl(storage, access)
                    };

                    var mainForm = new MainForm(modules);

                    // Фоновый поток ждет сигнала от повторного запуска .exe и разворачивает окно
                    using (var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName))
                    {
                        var listenerThread = new Thread(() =>
                        {
                            while (true)
                            {
                                showEvent.WaitOne();
                                if (mainForm.IsDisposed) break;
                                try
                                {
                                    mainForm.BeginInvoke(new Action(() => mainForm.RestoreFromTray()));
                                }
                                catch { }
                            }
                        })
                        { IsBackground = true };
                        listenerThread.Start();

                        Application.Run(mainForm);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowCopyableError(ex);
            }
        }

        private static void ShowCopyableError(Exception ex)
        {
            using (var errForm = new Form())
            {
                errForm.Text = "Ошибка приложения WorkSpace";
                errForm.Size = new Size(620, 420);
                errForm.StartPosition = FormStartPosition.CenterScreen;

                var txt = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    WordWrap = false,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9.5f),
                    Text = ex.ToString()
                };

                var btnCopy = new Button
                {
                    Text = "Скопировать текст ошибки в буфер обмена",
                    Dock = DockStyle.Bottom,
                    Height = 35
                };
                btnCopy.Click += (s, e) =>
                {
                    Clipboard.SetText(txt.Text);
                    btnCopy.Text = "Скопировано!";
                };

                errForm.Controls.Add(txt);
                errForm.Controls.Add(btnCopy);
                errForm.ShowDialog();
            }
        }
    }
}
