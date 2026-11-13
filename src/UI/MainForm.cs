using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WorkSpace.Core;

namespace WorkSpace.UI
{
    public class MainForm : Form
    {
        private readonly List<IWorkspaceModule> _modules;
        private readonly NotifyIcon _trayIcon;
        private readonly Timer _syncTimer;
        private bool _forceClose = false;

        public MainForm(List<IWorkspaceModule> modules)
        {
            _modules = modules;

            Text = string.Format("Рабочее место — {0}", AppConfig.CurrentUser);
            Size = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;

            // Вкладки для всех зарегистрированных модулей
            var tabs = new TabControl { Dock = DockStyle.Fill };
            foreach (var mod in _modules)
            {
                var page = new TabPage(mod.Title);
                page.Controls.Add(mod.View);
                tabs.TabPages.Add(page);
            }
            Controls.Add(tabs);

            // Иконка в системном трее (рядом с часами)
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Открыть рабочее место", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add("Выход из программы", null, (s, e) => { _forceClose = true; Application.Exit(); });

            Icon appIcon = IconHelper.CreateAppIcon("WS");
            this.Icon = appIcon;

            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "Рабочее место сотрудника",
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            // Фоновая синхронизация с сетевой папкой каждые 3 секунды
            _syncTimer = new Timer { Interval = 3000 };
            _syncTimer.Tick += (s, e) =>
            {
                foreach (var mod in _modules) mod.RefreshData();
            };
            _syncTimer.Start();
        }

        public void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        // Защита от случайного закрытия крестиком — сворачиваем в трей
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_forceClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _trayIcon.ShowBalloonTip(2000, "Программа свернута", "Трекер продолжает работать в трее.", ToolTipIcon.Info);
                return;
            }
            _trayIcon.Visible = false;
            base.OnFormClosing(e);
        }
    }
}
