using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace EyeGuard
{
    internal static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(15, 19, 25);
        public static readonly Color Surface = Color.FromArgb(22, 27, 35);
        public static readonly Color Card = Color.FromArgb(29, 36, 46);
        public static readonly Color CardHover = Color.FromArgb(38, 47, 60);
        public static readonly Color Line = Color.FromArgb(44, 54, 68);
        public static readonly Color Text = Color.FromArgb(230, 237, 247);
        public static readonly Color Muted = Color.FromArgb(143, 160, 181);
        public static readonly Color Accent = Color.FromArgb(63, 214, 176);
        public static readonly Color AccentDark = Color.FromArgb(16, 64, 56);
        public static readonly Color Warm = Color.FromArgb(255, 180, 92);
        public static readonly Color Danger = Color.FromArgb(255, 107, 107);

        public static Font CreateFont(float size, FontStyle style)
        {
            return CreateFont("Microsoft YaHei UI", size, style);
        }

        public static Font CreateFont(string family, float size, FontStyle style)
        {
            return CreateFontPx(family, size * 96f / 72f, style);
        }

        public static Font CreateFontPx(float pixelSize, FontStyle style)
        {
            return CreateFontPx("Microsoft YaHei UI", pixelSize, style);
        }

        public static Font CreateFontPx(string family, float pixelSize, FontStyle style)
        {
            try
            {
                Font font = new Font(family, pixelSize, style, GraphicsUnit.Pixel);
                if (string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }
                font.Dispose();
            }
            catch
            {
            }
            return new Font("Segoe UI", pixelSize, style, GraphicsUnit.Pixel);
        }

        public static Color Mix(Color a, Color b, double t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            int r = (int)Math.Round(a.R + (b.R - a.R) * t);
            int g = (int)Math.Round(a.G + (b.G - a.G) * t);
            int bl = (int)Math.Round(a.B + (b.B - a.B) * t);
            int al = (int)Math.Round(a.A + (b.A - a.A) * t);
            return Color.FromArgb(al, r, g, bl);
        }

        public static GraphicsPath RoundRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            if (d <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void EnableSmooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        public static Icon CreateStateIcon(Color ring, Color core)
        {
            const int size = 32;
            using (Bitmap bmp = new Bitmap(size, size))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    EnableSmooth(g);
                    g.Clear(Color.Transparent);

                    using (Pen pen = new Pen(ring, 3.4f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawArc(pen, 5.5f, 5.5f, size - 11, size - 11, -90f, 359f);
                    }

                    using (SolidBrush fill = new SolidBrush(ring))
                    {
                        g.FillEllipse(fill, 13.5f, 13.5f, 5f, 5f);
                    }

                    using (SolidBrush dot = new SolidBrush(core))
                    {
                        g.FillEllipse(dot, 15f, 15f, 2f, 2f);
                    }
                }

                IntPtr handle = bmp.GetHicon();
                try
                {
                    using (Icon raw = Icon.FromHandle(handle))
                    {
                        return (Icon)raw.Clone();
                    }
                }
                finally
                {
                    Native.DestroyIcon(handle);
                }
            }
        }

        public static Bitmap CreateLogo(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                EnableSmooth(g);
                g.Clear(Color.Transparent);

                RectangleF rect = new RectangleF(0.5f, 0.5f, size - 1f, size - 1f);
                using (GraphicsPath path = RoundRect(rect, Math.Max(2f, size * 0.235f)))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(96, 235, 201),
                    Color.FromArgb(24, 158, 132),
                    62f))
                {
                    g.FillPath(brush, path);
                }

                float cx = size / 2f;
                float cy = size * 0.505f;
                float eyeWidth = size * 0.60f;
                float eyeHeight = size * 0.30f;
                Color ink = Color.FromArgb(10, 30, 34);

                using (Pen pen = new Pen(ink, Math.Max(1.15f, size * 0.072f)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;

                    using (GraphicsPath eyePath = new GraphicsPath())
                    {
                        eyePath.AddBezier(
                            cx - eyeWidth / 2, cy,
                            cx - eyeWidth * 0.17f, cy - eyeHeight,
                            cx + eyeWidth * 0.17f, cy - eyeHeight,
                            cx + eyeWidth / 2, cy);
                        eyePath.AddBezier(
                            cx + eyeWidth / 2, cy,
                            cx + eyeWidth * 0.17f, cy + eyeHeight,
                            cx - eyeWidth * 0.17f, cy + eyeHeight,
                            cx - eyeWidth / 2, cy);
                        g.DrawPath(pen, eyePath);
                    }
                }

                float pr = Math.Max(1.4f, size * 0.098f);
                using (SolidBrush pupil = new SolidBrush(ink))
                {
                    g.FillEllipse(pupil, cx - pr, cy - pr, pr * 2f, pr * 2f);
                }

                if (size >= 24)
                {
                    float hr = Math.Max(0.8f, size * 0.030f);
                    using (SolidBrush highlight = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                    {
                        g.FillEllipse(highlight, cx - pr * 0.40f - hr, cy - pr * 0.40f - hr, hr * 2f, hr * 2f);
                    }
                }
            }
            return bmp;
        }
    }

    internal class FlatButton : Control
    {
        private bool _hover;
        private bool _down;
        private bool _primary;

        public bool Primary
        {
            get { return _primary; }
            set { _primary = value; Invalidate(); }
        }

        public int CornerRadius { get; set; }

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
            Cursor = Cursors.Hand;
            CornerRadius = 10;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _down = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _down = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _down = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color fill;
            Color border;
            Color text;
            using (GraphicsPath path = Theme.RoundRect(rect, CornerRadius))
            {
                if (_primary)
                {
                    fill = _hover ? Theme.Mix(Theme.Accent, Color.White, 0.14) : Theme.Accent;
                    if (_down) fill = Theme.Mix(Theme.Accent, Color.Black, 0.12);
                    border = fill;
                    text = Color.FromArgb(10, 32, 28);
                }
                else
                {
                    fill = _down ? Theme.Mix(Theme.Card, Color.Black, 0.10) : (_hover ? Theme.CardHover : Theme.Card);
                    border = Theme.Line;
                    text = ForeColor;
                }

                if (!Enabled)
                {
                    fill = Theme.Mix(fill, Theme.Bg, 0.55);
                    text = Theme.Muted;
                    border = Theme.Mix(border, Theme.Bg, 0.55);
                }

                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(border, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                                    TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text, flags);
        }
    }

    internal class RingView : Control
    {
        private double _progress;
        private string _caption = "";
        private string _sub = "";
        private Color _ringColor = Theme.Accent;
        private Font _bigFont;
        private Font _subFont;

        public double Progress
        {
            get { return _progress; }
            set
            {
                if (value < 0) value = 0;
                if (value > 1) value = 1;
                if (Math.Abs(_progress - value) > 0.0005)
                {
                    _progress = value;
                    Invalidate();
                }
            }
        }

        public string Caption
        {
            get { return _caption; }
            set { if (_caption != value) { _caption = value; Invalidate(); } }
        }

        public string SubCaption
        {
            get { return _sub; }
            set { if (_sub != value) { _sub = value; Invalidate(); } }
        }

        public Color RingColor
        {
            get { return _ringColor; }
            set { _ringColor = value; Invalidate(); }
        }

        public RingView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.CreateFont(9f, FontStyle.Regular);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            DisposeFonts();
            base.OnFontChanged(e);
        }

        private void DisposeFonts()
        {
            if (_bigFont != null) { _bigFont.Dispose(); _bigFont = null; }
            if (_subFont != null) { _subFont.Dispose(); _subFont = null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeFonts();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            int side = Math.Min(Width, Height);
            float thickness = Math.Max(7f, side * 0.038f);
            float pad = thickness / 2f + 6f;
            RectangleF ring = new RectangleF((Width - side) / 2f + pad, (Height - side) / 2f + pad,
                                             side - pad * 2f, side - pad * 2f);

            using (Pen track = new Pen(Color.FromArgb(38, 47, 60), thickness))
            {
                track.StartCap = LineCap.Round;
                track.EndCap = LineCap.Round;
                g.DrawArc(track, ring, 0f, 360f);
            }

            if (_progress > 0.001)
            {
                using (Pen pen = new Pen(_ringColor, thickness))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, ring, -90f, (float)(360.0 * _progress));
                }
            }

            if (_bigFont == null)
            {
                _bigFont = Theme.CreateFontPx(Font.FontFamily.Name, Math.Max(26f, side * 0.155f), FontStyle.Bold);
                _subFont = Theme.CreateFontPx(Font.FontFamily.Name, Math.Max(11f, side * 0.042f), FontStyle.Regular);
            }

            RectangleF textRect = new RectangleF(0, Height / 2f - side * 0.13f, Width, side * 0.18f);
            TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                     TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(g, _caption, _bigFont, Rectangle.Round(textRect), Theme.Text, center);

            RectangleF subRect = new RectangleF(0, Height / 2f + side * 0.055f, Width, side * 0.09f);
            TextRenderer.DrawText(g, _sub, _subFont, Rectangle.Round(subRect), Theme.Muted, center);
        }
    }

    internal class DarkForm : Form
    {
        public DarkForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.CreateFont(9f, FontStyle.Regular);
            DoubleBuffered = true;

            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null) Icon = appIcon;
            }
            catch
            {
            }
        }

        private float _uiScale = 1f;
        private bool _scaleApplied;

        protected float UiScale
        {
            get { return _uiScale; }
        }

        protected int S(int value)
        {
            return (int)Math.Round(value * _uiScale);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_scaleApplied) return;
            _scaleApplied = true;

            float dpi = 96f;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpi = g.DpiX;
                }
            }
            catch
            {
            }

            float scale = dpi / 96f;
            if (scale < 1f) scale = 1f;

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            float limit = Math.Max(0.85f, (area.Height - 60f) / Math.Max(1f, (float)Height));
            if (scale > limit) scale = limit;

            _uiScale = scale;
            if (Math.Abs(scale - 1f) > 0.001f)
            {
                Scale(new SizeF(scale, scale));
            }

            if (StartPosition == FormStartPosition.CenterScreen)
            {
                Location = new Point(area.Left + (area.Width - Width) / 2,
                                     area.Top + (area.Height - Height) / 2);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height > 0)
            {
                using (GraphicsPath path = Theme.RoundRect(new RectangleF(0, 0, Width, Height), 14f))
                {
                    Region old = Region;
                    Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Color.FromArgb(38, 47, 60), 1f))
            {
                RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                using (GraphicsPath path = Theme.RoundRect(rect, 14f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected void EnableDrag(Control control)
        {
            control.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Native.ReleaseCapture();
                    Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, Native.HTCAPTION, 0);
                }
            };
        }
    }

    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer()
            : base(new DarkColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Theme.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen pen = new Pen(Theme.Line))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Theme.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Theme.Muted;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (Pen pen = new Pen(Theme.Line))
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;
            Rectangle rect = new Rectangle(4, 0, e.Item.Width - 8, e.Item.Height);
            using (GraphicsPath path = Theme.RoundRect(rect, 6f))
            using (SolidBrush brush = new SolidBrush(Theme.CardHover))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected { get { return Theme.CardHover; } }
            public override Color MenuItemBorder { get { return Theme.CardHover; } }
            public override Color MenuBorder { get { return Theme.Line; } }
            public override Color ToolStripDropDownBackground { get { return Theme.Surface; } }
            public override Color ImageMarginGradientBegin { get { return Theme.Surface; } }
            public override Color ImageMarginGradientMiddle { get { return Theme.Surface; } }
            public override Color ImageMarginGradientEnd { get { return Theme.Surface; } }
            public override Color SeparatorDark { get { return Theme.Line; } }
            public override Color SeparatorLight { get { return Theme.Line; } }
            public override Color CheckBackground { get { return Theme.AccentDark; } }
            public override Color CheckSelectedBackground { get { return Theme.AccentDark; } }
            public override Color MenuItemSelectedGradientBegin { get { return Theme.CardHover; } }
            public override Color MenuItemSelectedGradientEnd { get { return Theme.CardHover; } }
            public override Color MenuItemPressedGradientBegin { get { return Theme.Card; } }
            public override Color MenuItemPressedGradientEnd { get { return Theme.Card; } }
        }
    }
}
