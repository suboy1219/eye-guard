using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EyeGuard
{
    internal class OverlayForm : Form
    {
        private readonly bool _primary;
        private readonly Timer _anim;
        private FlatButton _skip;
        private int _restLeft;
        private int _restTotal;
        private float _phase;
        private bool _allowClose;
        private bool _skipEnabled;

        public event EventHandler SkipRequested;

        public OverlayForm(Screen screen, bool primary, bool allowSkip)
        {
            _primary = primary;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = screen.Bounds;
            BackColor = Color.FromArgb(10, 15, 21);
            KeyPreview = true;

            if (primary && allowSkip)
            {
                _skip = new FlatButton();
                _skip.Text = "跳过休息";
                _skip.Primary = false;
                _skip.Size = new Size(196, 44);
                _skip.Enabled = false;
                _skip.Click += delegate { RequestSkip(); };
                Controls.Add(_skip);
            }

            _phase = 0f;
            _anim = new Timer();
            _anim.Interval = 40;
            _anim.Tick += AnimTick;
            _anim.Start();
        }

        public void SetRest(int remaining, int total)
        {
            _restLeft = remaining;
            _restTotal = total;
            if (_skip != null)
            {
                int wait = Math.Max(0, 10 - (total - remaining));
                bool ready = wait <= 0;
                _skipEnabled = ready;
                _skip.Enabled = ready;
                _skip.Text = ready ? "跳过休息" : string.Format("跳过休息（{0}s）", wait);
                LayoutSkip();
            }
            Invalidate();
        }

        public void AllowClose()
        {
            _allowClose = true;
        }

        private void AnimTick(object sender, EventArgs e)
        {
            _phase += 0.055f;
            if (_phase > (float)(Math.PI * 2)) _phase -= (float)(Math.PI * 2);
            if (_primary) Invalidate();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Native.SetWindowPos(Handle, (IntPtr)(-1), Left, Top, Width, Height,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }

        private void LayoutSkip()
        {
            if (_skip == null) return;
            int side = Math.Min(Width, Height);
            int cx = Width / 2;
            int cy = Height / 2;
            int radius = (int)(side * 0.17f);
            _skip.Location = new Point(cx - _skip.Width / 2, cy + radius + (int)(side * 0.09f));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutSkip();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                RequestSkip();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void RequestSkip()
        {
            if (!_skipEnabled) return;
            if (SkipRequested != null) SkipRequested(this, EventArgs.Empty);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                RequestSkip();
            }
            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            Rectangle bounds = ClientRectangle;
            using (LinearGradientBrush back = new LinearGradientBrush(bounds,
                Color.FromArgb(9, 13, 19), Color.FromArgb(13, 22, 29), 90f))
            {
                g.FillRectangle(back, bounds);
            }

            int side = Math.Min(Width, Height);
            int cx = Width / 2;
            int cy = Height / 2;
            int radius = (int)(side * 0.17f);

            float halo = (float)(0.5 + 0.5 * Math.Sin(_phase));
            int haloRadius = radius + (int)(side * (0.05f + 0.03f * halo));
            using (GraphicsPath haloPath = new GraphicsPath())
            {
                haloPath.AddEllipse(cx - haloRadius, cy - haloRadius, haloRadius * 2, haloRadius * 2);
                using (PathGradientBrush glow = new PathGradientBrush(haloPath))
                {
                    glow.CenterColor = Color.FromArgb(_primary ? 46 : 26, Theme.Accent);
                    glow.SurroundColors = new Color[] { Color.FromArgb(0, Theme.Accent) };
                    g.FillPath(glow, haloPath);
                }
            }

            float ringThickness = Math.Max(3f, side * 0.005f);
            RectangleF ringRect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);
            using (Pen track = new Pen(Color.FromArgb(34, 44, 56), ringThickness))
            {
                g.DrawEllipse(track, ringRect);
            }

            if (_restTotal > 0)
            {
                float part = _restLeft / (float)_restTotal;
                part = Math.Max(0f, Math.Min(1f, part));
                if (part > 0.995f) part = 1f;
                using (Pen pen = new Pen(Theme.Accent, ringThickness))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    if (part >= 0.999f)
                    {
                        g.DrawEllipse(pen, ringRect);
                    }
                    else
                    {
                        g.DrawArc(pen, ringRect, -90f, 360f * part);
                    }
                }
            }

            string title = _primary ? "该让眼睛歇一会儿了" : "护眼休息";
            string[] adviceLines = _primary
                ? new string[] { "看向 6 米以外的物体，放松睫状肌", "也可以闭目养神，让眼球保持湿润" }
                : new string[] { "闭目养神" };
            string clock = FormatClock(_restLeft);

            using (Font titleFont = Theme.CreateFontPx(Font.FontFamily.Name, Math.Max(20f, side * 0.021f), FontStyle.Bold))
            using (Font clockFont = Theme.CreateFontPx(Font.FontFamily.Name, Math.Max(46f, side * 0.072f), FontStyle.Bold))
            using (Font bodyFont = Theme.CreateFontPx(Font.FontFamily.Name, Math.Max(15f, side * 0.0155f), FontStyle.Regular))
            using (Font tinyFont = Theme.CreateFontPx(Font.FontFamily.Name, Math.Max(11f, side * 0.0105f), FontStyle.Regular))
            {
                Rectangle titleRect = new Rectangle(cx - 400, cy - radius - (int)(side * 0.105f), 800, 44);
                TextRenderer.DrawText(g, title, titleFont, titleRect, Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                Rectangle clockRect = new Rectangle(cx - radius, cy - (int)(side * 0.062f), radius * 2, (int)(side * 0.095f));
                TextRenderer.DrawText(g, clock, clockFont, clockRect, Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                Rectangle bodyRect = new Rectangle(cx - 360, cy + (int)(side * 0.082f), 720, 84);
                int lineHeight = (int)Math.Round(bodyFont.Height * 1.6f);
                for (int i = 0; i < adviceLines.Length; i++)
                {
                    Rectangle lineRect = new Rectangle(bodyRect.X, bodyRect.Y + i * lineHeight, bodyRect.Width, lineHeight);
                    TextRenderer.DrawText(g, adviceLines[i], bodyFont, lineRect, Theme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }

                if (_primary && _skip != null)
                {
                    string hint = _skipEnabled ? "按 ESC 也可以跳过" : "休息一下，稍后可以跳过";
                    Rectangle hintRect = new Rectangle(cx - 300, _skip.Bottom + 16, 600, 28);
                    TextRenderer.DrawText(g, hint, tinyFont, hintRect, Theme.Mix(Theme.Muted, Theme.Bg, 0.25),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
        }

        private static string FormatClock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_anim != null) _anim.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
