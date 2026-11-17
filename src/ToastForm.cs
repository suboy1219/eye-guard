using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EyeGuard
{
    internal class ToastForm : Form
    {
        private readonly Timer _life;
        private readonly Timer _fade;
        private int _remainingMs;
        private readonly Icon _icon;
        private readonly string _title;
        private readonly string _message;
        private readonly Color _accent;

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000;
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        public ToastForm(string title, string message, Icon icon, Color accent)
        {
            _title = title;
            _message = message;
            _icon = icon;
            _accent = accent;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Surface;
            Opacity = 0.0;
            Size = new Size(356, 96);

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 20, area.Bottom - Height - 20);

            _remainingMs = 4200;
            _life = new Timer();
            _life.Interval = 100;
            _life.Tick += LifeTick;
            _fade = new Timer();
            _fade.Interval = 24;
            _fade.Tick += FadeTick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _life.Start();
            _fade.Start();
        }

        private void LifeTick(object sender, EventArgs e)
        {
            _remainingMs -= _life.Interval;
            if (_remainingMs <= 0)
            {
                _life.Stop();
                _fade.Start();
            }
        }

        private void FadeTick(object sender, EventArgs e)
        {
            if (_remainingMs > 2500)
            {
                if (Opacity < 0.97) Opacity = Math.Min(0.97, Opacity + 0.12);
                else _fade.Stop();
            }
            else
            {
                if (Opacity > 0.08) Opacity = Math.Max(0.0, Opacity - 0.10);
                else
                {
                    _fade.Stop();
                    Close();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            using (GraphicsPath path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 10f))
            {
                using (SolidBrush brush = new SolidBrush(Theme.Surface))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(Theme.Line))
                {
                    g.DrawPath(pen, path);
                }
            }

            using (SolidBrush bar = new SolidBrush(_accent))
            {
                g.FillRectangle(bar, 0, 18, 3, Height - 36);
            }

            if (_icon != null)
            {
                g.DrawIcon(_icon, new Rectangle(18, Height / 2 - 12, 24, 24));
            }

            using (Font titleFont = Theme.CreateFont(Font.FontFamily.Name, 10.5f, FontStyle.Bold))
            using (Font bodyFont = Theme.CreateFont(Font.FontFamily.Name, 9f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, _title, titleFont, new Rectangle(54, 16, Width - 70, 26),
                    Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                TextRenderer.DrawText(g, _message, bodyFont, new Rectangle(54, 42, Width - 68, 40),
                    Theme.Muted, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_life != null) _life.Dispose();
                if (_fade != null) _fade.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
