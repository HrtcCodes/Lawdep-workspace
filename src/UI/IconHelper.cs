using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace WorkSpace.UI
{
    public static class IconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon CreateAppIcon(string text = "RG", Color? bgColor = null)
        {
            Color fill = bgColor ?? Color.FromArgb(51, 164, 87); // Приятный корпоративный зеленый

            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                // Рисуем квадрат со скругленными углами
                using (var brush = new SolidBrush(fill))
                using (var path = CreateRoundedRect(new Rectangle(1, 1, 30, 30), 6))
                {
                    g.FillPath(brush, path);
                }

                // Рисуем текст (буквы WS или счетчик задач) по центру
                using (var font = new Font("Segoe UI", text.Length > 2 ? 10f : 12f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(text, font, textBrush, new RectangleF(0, 1, 32, 32), sf);
                }

                // Превращаем картинку в системную иконку Windows
                IntPtr hIcon = bmp.GetHicon();
                Icon result = (Icon)Icon.FromHandle(hIcon).Clone();
                DestroyIcon(hIcon); // Освобождаем неуправляемую память Windows
                return result;
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
