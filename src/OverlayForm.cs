using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EyeGuard
{
    internal class OverlayForm : Form
    {
        private readonly bool _primary;
        private readonly RestMessage _message;
        private readonly Timer _anim;
        private FlatButton _skip;
        private int _restLeft;
        private int _restTotal;
        private float _phase;
        private bool _allowClose;
        private bool _skipEnabled;

        public event EventHandler SkipRequested;

        public OverlayForm(Screen screen, bool primary, bool allowSkip, RestMessage message)
        {
            _primary = primary;
            _message = message;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = screen.Bounds;
            BackColor = Color.FromArgb(10, 15, 21);
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.CreateFont(9f, FontStyle.Regular);
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
            _skip.Bounds = GetRestLayout().Skip;
        }

        private struct RestLayout
        {
            public Rectangle Title, Ring, Clock, Advice, Skip, Hint;
        }

        private RestLayout GetRestLayout()
        {
            int side = Math.Min(ClientSize.Width, ClientSize.Height);
            int width = Math.Max(1, Math.Min(ClientSize.Width - 48, Math.Max(420, (int)(side * 0.60f))));
            int x = (ClientSize.Width - width) / 2;
            int diameter = Math.Max(1, (int)(side * 0.34f));
            int titleGap = Math.Max(20, (int)(side * 0.035f));
            int adviceGap = Math.Max(20, (int)(side * 0.028f));
            int buttonGap = Math.Max(20, (int)(side * 0.028f));
            const int hintGap = 12;
            TextFormatFlags wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;

            using (Font titleFont = Theme.CreateFontPx(Math.Max(20f, side * 0.021f), FontStyle.Bold))
            using (Font bodyFont = Theme.CreateFontPx(Math.Max(15f, side * 0.0155f), FontStyle.Regular))
            using (Font tinyFont = Theme.CreateFontPx(Math.Max(11f, side * 0.0105f), FontStyle.Regular))
            {
                int titleHeight = TextRenderer.MeasureText(_message.Title, titleFont, new Size(width, int.MaxValue), wrap).Height;
                int bodyHeight = TextRenderer.MeasureText(_message.Advice, bodyFont, new Size(width, int.MaxValue), wrap).Height;
                int hintHeight = tinyFont.Height + 4;
                int totalHeight = titleHeight + titleGap + diameter + adviceGap + bodyHeight;
                if (_skip != null) totalHeight += buttonGap + _skip.Height + hintGap + hintHeight;

                int y = (ClientSize.Height - totalHeight) / 2;
                RestLayout layout = new RestLayout();
                layout.Title = new Rectangle(x, y, width, titleHeight);
                layout.Ring = new Rectangle((ClientSize.Width - diameter) / 2,
                    layout.Title.Bottom + titleGap, diameter, diameter);
                layout.Clock = layout.Ring;
                // Keep all advice below the ring, including wrapped lines on narrow screens.
                layout.Advice = new Rectangle(x, layout.Ring.Bottom + adviceGap, width, bodyHeight);
                if (_skip != null)
                {
                    layout.Skip = new Rectangle((ClientSize.Width - _skip.Width) / 2,
                        layout.Advice.Bottom + buttonGap, _skip.Width, _skip.Height);
                    layout.Hint = new Rectangle(x, layout.Skip.Bottom + hintGap, width, hintHeight);
                }
                return layout;
            }
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

            int side = Math.Min(ClientSize.Width, ClientSize.Height);
            RestLayout layout = GetRestLayout();
            int radius = layout.Ring.Width / 2;
            int cx = layout.Ring.Left + radius;
            int cy = layout.Ring.Top + radius;

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
            RectangleF ringRect = layout.Ring;
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

            string clock = FormatClock(_restLeft);

            using (Font titleFont = Theme.CreateFontPx(Math.Max(20f, side * 0.021f), FontStyle.Bold))
            using (Font clockFont = Theme.CreateFontPx(Math.Min(Math.Max(32f, side * 0.072f),
                layout.Ring.Width * 0.23f), FontStyle.Bold))
            using (Font bodyFont = Theme.CreateFontPx(Math.Max(15f, side * 0.0155f), FontStyle.Regular))
            using (Font tinyFont = Theme.CreateFontPx(Math.Max(11f, side * 0.0105f), FontStyle.Regular))
            {
                TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding;
                TextRenderer.DrawText(g, _message.Title, titleFont, layout.Title, Theme.Muted,
                    center | TextFormatFlags.WordBreak);
                TextRenderer.DrawText(g, clock, clockFont, layout.Clock, Theme.Text,
                    center | TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g, _message.Advice, bodyFont, layout.Advice, Theme.Muted,
                    center | TextFormatFlags.WordBreak);

                if (_primary && _skip != null)
                {
                    string hint = _skipEnabled ? "按 ESC 也可以跳过" : "休息一下，稍后可以跳过";
                    TextRenderer.DrawText(g, hint, tinyFont, layout.Hint, Theme.Mix(Theme.Muted, Theme.Bg, 0.25), center);
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
