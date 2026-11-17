using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EyeGuard
{
    internal class SettingsForm : DarkForm
    {
        private readonly Config _draft;
        private readonly EyeGuardContext _app;
        private readonly Panel _titleBar;
        private readonly NumberBox _work;
        private readonly NumberBox _rest;
        private readonly Slider _blue;
        private readonly Label _blueValue;
        private readonly DarkCheck _overlay;
        private readonly DarkCheck _skip;
        private readonly DarkCheck _warn;
        private readonly DarkCheck _micro;
        private readonly DarkCheck _autoStart;

        public SettingsForm(Config draft, EyeGuardContext app)
        {
            _draft = draft;
            _app = app;
            Text = "设置";
            ClientSize = new Size(420, 620);
            StartPosition = FormStartPosition.CenterParent;

            _titleBar = new Panel();
            _titleBar.BackColor = Theme.Bg;
            _titleBar.Location = new Point(0, 0);
            _titleBar.Size = new Size(420, 58);
            EnableDrag(_titleBar);
            Controls.Add(_titleBar);

            Label title = new Label();
            title.Text = "设置";
            title.Font = Theme.CreateFont(11.5f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.BackColor = Color.Transparent;
            title.Location = new Point(24, 16);
            title.Size = new Size(160, 28);
            title.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Native.ReleaseCapture();
                    Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, Native.HTCAPTION, 0);
                }
            };
            _titleBar.Controls.Add(title);

            FlatButton close = new FlatButton();
            close.Text = "×";
            close.CornerRadius = 8;
            close.Size = new Size(30, 28);
            close.Location = new Point(370, 16);
            close.Click += delegate { DialogResult = DialogResult.Cancel; };
            _titleBar.Controls.Add(close);

            AddSectionLabel("休息节奏", 74);

            AddRowLabel("工作间隔", 108);
            _work = new NumberBox();
            _work.Minimum = 1;
            _work.Maximum = 180;
            _work.Value = _draft.WorkMinutes;
            _work.Suffix = "分钟";
            _work.Location = new Point(212, 100);
            _work.Size = new Size(184, 36);
            Controls.Add(_work);

            AddRowLabel("休息时长", 150);
            _rest = new NumberBox();
            _rest.Minimum = 1;
            _rest.Maximum = 60;
            _rest.Value = _draft.RestMinutes;
            _rest.Suffix = "分钟";
            _rest.Location = new Point(212, 142);
            _rest.Size = new Size(184, 36);
            Controls.Add(_rest);

            AddSeparator(194);

            AddSectionLabel("蓝光过滤", 210);
            Label blueHint = new Label();
            blueHint.Text = "降低屏幕蓝光，减少夜间用眼疲劳。建议 30% – 60%。";
            blueHint.ForeColor = Theme.Muted;
            blueHint.BackColor = Color.Transparent;
            blueHint.Font = Theme.CreateFont(8f, FontStyle.Regular);
            blueHint.Location = new Point(24, 234);
            blueHint.Size = new Size(300, 20);
            Controls.Add(blueHint);

            _blueValue = new Label();
            _blueValue.Text = _draft.BlueLight + "%";
            _blueValue.ForeColor = Theme.Accent;
            _blueValue.BackColor = Color.Transparent;
            _blueValue.Font = Theme.CreateFont(9.5f, FontStyle.Bold);
            _blueValue.TextAlign = ContentAlignment.MiddleRight;
            _blueValue.Location = new Point(330, 232);
            _blueValue.Size = new Size(66, 22);
            Controls.Add(_blueValue);

            _blue = new Slider();
            _blue.Minimum = 0;
            _blue.Maximum = 100;
            _blue.Value = _draft.BlueLight;
            _blue.Location = new Point(20, 260);
            _blue.Size = new Size(380, 36);
            _blue.ValueChanged += delegate
            {
                _draft.BlueLight = _blue.Value;
                _blueValue.Text = _blue.Value + "%";
                _app.PreviewBlueLight(_blue.Value);
            };
            Controls.Add(_blue);

            AddSeparator(312);

            AddSectionLabel("提醒方式", 326);

            _overlay = new DarkCheck();
            _overlay.Text = "休息时显示全屏遮罩";
            _overlay.Checked = _draft.ShowOverlay;
            _overlay.Location = new Point(24, 352);
            _overlay.Size = new Size(370, 28);
            Controls.Add(_overlay);

            _skip = new DarkCheck();
            _skip.Text = "允许跳过休息（10 秒后）";
            _skip.Checked = _draft.AllowSkip;
            _skip.Location = new Point(24, 386);
            _skip.Size = new Size(370, 28);
            Controls.Add(_skip);

            _warn = new DarkCheck();
            _warn.Text = "休息前 30 秒提示";
            _warn.Checked = _draft.NotifyBefore;
            _warn.Location = new Point(24, 420);
            _warn.Size = new Size(370, 28);
            Controls.Add(_warn);

            _micro = new DarkCheck();
            _micro.Text = "20-20-20 微提醒（每小时站起来走走）";
            _micro.Checked = _draft.MicroBreak;
            _micro.Location = new Point(24, 454);
            _micro.Size = new Size(370, 28);
            Controls.Add(_micro);

            AddSeparator(496);

            _autoStart = new DarkCheck();
            _autoStart.Text = "开机自动启动（仅写入当前用户）";
            _autoStart.Checked = _draft.AutoStart;
            _autoStart.Location = new Point(24, 510);
            _autoStart.Size = new Size(370, 28);
            Controls.Add(_autoStart);

            FlatButton save = new FlatButton();
            save.Text = "保存并应用";
            save.Primary = true;
            save.Location = new Point(24, 556);
            save.Size = new Size(180, 44);
            save.Click += delegate { Save(); };
            Controls.Add(save);

            FlatButton cancel = new FlatButton();
            cancel.Text = "取消";
            cancel.Location = new Point(216, 556);
            cancel.Size = new Size(180, 44);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
        }

        private void Save()
        {
            _draft.WorkMinutes = _work.Value;
            _draft.RestMinutes = _rest.Value;
            _draft.BlueLight = _blue.Value;
            _draft.ShowOverlay = _overlay.Checked;
            _draft.AllowSkip = _skip.Checked;
            _draft.NotifyBefore = _warn.Checked;
            _draft.MicroBreak = _micro.Checked;
            _draft.AutoStart = _autoStart.Checked;
            DialogResult = DialogResult.OK;
        }

        private void AddSectionLabel(string text, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Theme.Text;
            label.BackColor = Color.Transparent;
            label.Font = Theme.CreateFont(10f, FontStyle.Bold);
            label.Location = new Point(24, top);
            label.Size = new Size(360, 24);
            Controls.Add(label);
        }

        private void AddRowLabel(string text, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Theme.Muted;
            label.BackColor = Color.Transparent;
            label.Font = Theme.CreateFont(9f, FontStyle.Regular);
            label.Location = new Point(24, top);
            label.Size = new Size(170, 24);
            Controls.Add(label);
        }

        private void AddSeparator(int top)
        {
            Panel line = new Panel();
            line.BackColor = Theme.Line;
            line.Location = new Point(24, top);
            line.Size = new Size(372, 1);
            Controls.Add(line);
        }
    }

    internal class NumberBox : Control
    {
        private int _value;
        private int _min;
        private int _max = 100;
        private bool _minusHover;
        private bool _plusHover;

        public int Minimum
        {
            get { return _min; }
            set { _min = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _max; }
            set { _max = value; Invalidate(); }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int v = value;
                if (v < _min) v = _min;
                if (v > _max) v = _max;
                if (_value != v)
                {
                    _value = v;
                    Invalidate();
                }
            }
        }

        public string Suffix { get; set; }

        public NumberBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Suffix = "";
            Font = Theme.CreateFont(9f, FontStyle.Regular);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _minusHover = e.X < 40;
            _plusHover = e.X > Width - 40;
            Invalidate();
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _minusHover = false;
            _plusHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.X < 40) Value = Value - 1;
                else if (e.X > Width - 40) Value = Value + 1;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Value = Value + (e.Delta > 0 ? 1 : -1);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            using (GraphicsPath path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 9f))
            {
                using (SolidBrush brush = new SolidBrush(Theme.Card))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(Theme.Line))
                {
                    g.DrawPath(pen, path);
                }
            }

            using (SolidBrush hover = new SolidBrush(Theme.CardHover))
            {
                if (_minusHover) g.FillRectangle(hover, 6, 6, 28, Height - 12);
                if (_plusHover) g.FillRectangle(hover, Width - 34, 6, 28, Height - 12);
            }

            using (SolidBrush textBrush = new SolidBrush(Theme.Muted))
            using (Font signFont = Theme.CreateFont(Font.FontFamily.Name, 12f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, "−", signFont, new Rectangle(6, 0, 28, Height), Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "+", signFont, new Rectangle(Width - 34, 0, 28, Height), Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            string text = _value + " " + Suffix;
            TextRenderer.DrawText(g, text, Font, new Rectangle(40, 0, Width - 80, Height), Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    internal class Slider : Control
    {
        private int _value;
        private int _min;
        private int _max = 100;
        private bool _dragging;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get { return _min; }
            set { _min = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _max; }
            set { _max = value; Invalidate(); }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int v = value;
                if (v < _min) v = _min;
                if (v > _max) v = _max;
                if (_value == v) return;
                _value = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        public Slider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        private int TrackLeft
        {
            get { return 12; }
        }

        private int TrackWidth
        {
            get { return Math.Max(1, Width - 24); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                SetFromX(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            base.OnMouseUp(e);
        }

        private void SetFromX(int x)
        {
            double ratio = (x - TrackLeft) / (double)TrackWidth;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            Value = _min + (int)Math.Round(ratio * (_max - _min));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            float ratio = (_max > _min) ? (_value - _min) / (float)(_max - _min) : 0f;
            int trackY = Height / 2 - 3;
            Rectangle track = new Rectangle(TrackLeft, trackY, TrackWidth, 6);

            using (GraphicsPath path = Theme.RoundRect(track, 3f))
            using (SolidBrush brush = new SolidBrush(Theme.CardHover))
            {
                g.FillPath(brush, path);
            }

            int filled = (int)Math.Round(TrackWidth * ratio);
            if (filled > 0)
            {
                using (GraphicsPath path = Theme.RoundRect(new Rectangle(TrackLeft, trackY, filled, 6), 3f))
                using (SolidBrush brush = new SolidBrush(Theme.Accent))
                {
                    g.FillPath(brush, path);
                }
            }

            float knobX = TrackLeft + TrackWidth * ratio;
            using (SolidBrush halo = new SolidBrush(Color.FromArgb(36, Theme.Accent)))
            {
                g.FillEllipse(halo, knobX - 11, Height / 2f - 11, 22, 22);
            }
            using (SolidBrush knob = new SolidBrush(Theme.Accent))
            {
                g.FillEllipse(knob, knobX - 7, Height / 2f - 7, 14, 14);
            }
            using (SolidBrush center = new SolidBrush(Theme.Bg))
            {
                g.FillEllipse(center, knobX - 3, Height / 2f - 3, 6, 6);
            }
        }
    }

    internal class DarkCheck : Control
    {
        private bool _checked;

        public bool Checked
        {
            get { return _checked; }
            set { _checked = value; Invalidate(); }
        }

        public DarkCheck()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Theme.CreateFont(9f, FontStyle.Regular);
        }

        protected override void OnClick(EventArgs e)
        {
            _checked = !_checked;
            Invalidate();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            Rectangle box = new Rectangle(0, (Height - 20) / 2, 20, 20);
            using (GraphicsPath path = Theme.RoundRect(box, 6f))
            {
                using (SolidBrush brush = new SolidBrush(_checked ? Theme.Accent : Theme.Card))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(_checked ? Theme.Accent : Color.FromArgb(70, 84, 102)))
                {
                    g.DrawPath(pen, path);
                }
            }

            if (_checked)
            {
                using (Pen pen = new Pen(Theme.Bg, 2.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new PointF[]
                    {
                        new PointF(5.5f, Height / 2f),
                        new PointF(9f, Height / 2f + 3.5f),
                        new PointF(14.5f, Height / 2f - 4f)
                    });
                }
            }

            TextRenderer.DrawText(g, Text, Font, new Rectangle(32, 0, Width - 32, Height), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
