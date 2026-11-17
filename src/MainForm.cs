using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EyeGuard
{
    internal class MainForm : DarkForm
    {
        private readonly EyeGuardContext _app;
        private readonly Panel _titleBar;
        private readonly FlatButton _closeButton;
        private readonly FlatButton _minButton;
        private readonly StatusChip _chip;
        private readonly RingView _ring;
        private readonly StatCard _cardRests;
        private readonly StatCard _cardTime;
        private readonly FlatButton _restButton;
        private readonly FlatButton _pauseButton;
        private readonly FlatButton _settingsButton;
        private readonly Label _footer;

        public MainForm(EyeGuardContext app)
        {
            _app = app;
            Text = "护眼卫士";
            ClientSize = new Size(460, 648);
            ShowInTaskbar = true;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;

            _titleBar = new Panel();
            _titleBar.BackColor = Theme.Bg;
            _titleBar.Location = new Point(0, 0);
            _titleBar.Size = new Size(460, 64);
            EnableDrag(_titleBar);
            Controls.Add(_titleBar);

            PictureBox logo = new PictureBox();
            logo.Image = Theme.CreateLogo(32);
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.BackColor = Color.Transparent;
            logo.Location = new Point(22, 16);
            logo.Size = new Size(32, 32);
            logo.MouseDown += DragHandler;
            _titleBar.Controls.Add(logo);

            Label title = new Label();
            title.Text = "护眼卫士";
            title.ForeColor = Theme.Text;
            title.BackColor = Color.Transparent;
            title.Font = Theme.CreateFont(12.5f, FontStyle.Bold);
            title.Location = new Point(62, 12);
            title.Size = new Size(200, 26);
            title.MouseDown += DragHandler;
            _titleBar.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "EYEGUARD  ·  20-20-20";
            subtitle.ForeColor = Theme.Muted;
            subtitle.BackColor = Color.Transparent;
            subtitle.Font = Theme.CreateFont(7.5f, FontStyle.Regular);
            subtitle.Location = new Point(63, 36);
            subtitle.Size = new Size(220, 18);
            subtitle.MouseDown += DragHandler;
            _titleBar.Controls.Add(subtitle);

            _minButton = new FlatButton();
            _minButton.Text = "–";
            _minButton.CornerRadius = 8;
            _minButton.Size = new Size(30, 28);
            _minButton.Click += delegate { _app.HideMain(); };
            _titleBar.Controls.Add(_minButton);

            _closeButton = new FlatButton();
            _closeButton.Text = "×";
            _closeButton.CornerRadius = 8;
            _closeButton.Size = new Size(30, 28);
            _closeButton.Click += delegate { _app.HideMain(); };
            _titleBar.Controls.Add(_closeButton);

            _chip = new StatusChip();
            _chip.Size = new Size(118, 28);
            _chip.BackColor = Color.Transparent;
            Controls.Add(_chip);

            _ring = new RingView();
            _ring.Location = new Point(70, 104);
            _ring.Size = new Size(320, 320);
            _ring.BackColor = Color.Transparent;
            Controls.Add(_ring);

            _cardRests = new StatCard();
            _cardRests.TitleText = "今日休息";
            _cardRests.Location = new Point(22, 440);
            _cardRests.Size = new Size(200, 78);
            _cardRests.BackColor = Color.Transparent;
            Controls.Add(_cardRests);

            _cardTime = new StatCard();
            _cardTime.TitleText = "今日用眼";
            _cardTime.ValueColor = Theme.Accent;
            _cardTime.Location = new Point(238, 440);
            _cardTime.Size = new Size(200, 78);
            _cardTime.BackColor = Color.Transparent;
            Controls.Add(_cardTime);

            _restButton = new FlatButton();
            _restButton.Text = "立即休息";
            _restButton.Primary = true;
            _restButton.Location = new Point(22, 532);
            _restButton.Size = new Size(272, 46);
            _restButton.Click += delegate { _app.ToggleRest(); };
            Controls.Add(_restButton);

            _pauseButton = new FlatButton();
            _pauseButton.Text = "暂停";
            _pauseButton.Location = new Point(306, 532);
            _pauseButton.Size = new Size(132, 46);
            _pauseButton.Click += delegate { _app.TogglePause(); };
            Controls.Add(_pauseButton);

            _settingsButton = new FlatButton();
            _settingsButton.Text = "设置";
            _settingsButton.Location = new Point(22, 590);
            _settingsButton.Size = new Size(74, 30);
            _settingsButton.CornerRadius = 8;
            _settingsButton.Font = Theme.CreateFont(8.5f, FontStyle.Regular);
            _settingsButton.Click += delegate { _app.ShowSettings(); };
            Controls.Add(_settingsButton);

            _footer = new Label();
            _footer.Text = "每 20 分钟，看 6 米外 20 秒";
            _footer.ForeColor = Theme.Mix(Theme.Muted, Theme.Bg, 0.2);
            _footer.BackColor = Color.Transparent;
            _footer.TextAlign = ContentAlignment.MiddleRight;
            _footer.Font = Theme.CreateFont(8f, FontStyle.Regular);
            _footer.Location = new Point(100, 594);
            _footer.Size = new Size(338, 24);
            Controls.Add(_footer);

            _titleBar.Paint += TitleBarPaint;
        }

        private void TitleBarPaint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Theme.Line))
            {
                e.Graphics.DrawLine(pen, 22, _titleBar.Height - 1, _titleBar.Width - 22, _titleBar.Height - 1);
            }
        }

        private void DragHandler(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Native.ReleaseCapture();
                Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, Native.HTCAPTION, 0);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutHeader();
        }

        private void LayoutHeader()
        {
            if (_titleBar == null) return;
            _titleBar.Width = ClientSize.Width;
            _closeButton.Location = new Point(ClientSize.Width - S(44), S(18));
            _minButton.Location = new Point(ClientSize.Width - S(80), S(18));
            _chip.Location = new Point((ClientSize.Width - _chip.Width) / 2, S(76));
        }

        public void UpdateView()
        {
            if (IsDisposed) return;

            bool resting = _app.State == RunState.Resting;
            bool paused = _app.State == RunState.Paused;
            int seconds = resting ? _app.RestLeft : _app.WorkLeft;

            if (resting)
            {
                _chip.Text = "休息中";
                _chip.DotColor = Theme.Warm;
                _ring.Caption = FormatClock(seconds);
                _ring.SubCaption = "休息倒计时";
                _ring.RingColor = Theme.Warm;
                _ring.Progress = _app.RestTotal > 0 ? _app.RestLeft / (double)_app.RestTotal : 0;
                _restButton.Text = "结束休息";
                _pauseButton.Text = "暂停";
                _pauseButton.Enabled = false;
            }
            else if (paused)
            {
                _chip.Text = "已暂停";
                _chip.DotColor = Theme.Muted;
                _ring.Caption = FormatClock(seconds);
                _ring.SubCaption = "计时已暂停";
                _ring.RingColor = Theme.Muted;
                _ring.Progress = 0;
                _restButton.Text = "立即休息";
                _pauseButton.Text = "继续";
                _pauseButton.Enabled = true;
            }
            else
            {
                _chip.Text = "专注中";
                _chip.DotColor = Theme.Accent;
                _ring.Caption = FormatClock(seconds);
                _ring.SubCaption = "距离下次休息";
                _ring.RingColor = Theme.Accent;
                double total = Math.Max(1, _app.Cfg.WorkMinutes * 60);
                _ring.Progress = 1.0 - Math.Max(0, _app.WorkLeft) / total;
                _restButton.Text = "立即休息";
                _pauseButton.Text = "暂停";
                _pauseButton.Enabled = true;
            }

            _cardRests.ValueText = _app.Cfg.TodayRests + " 次";
            _cardTime.ValueText = FormatDuration(_app.Cfg.TodayWorkSeconds);
        }

        private static string FormatClock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        }

        private static string FormatDuration(int seconds)
        {
            if (seconds < 60) return "不足 1m";
            int minutes = seconds / 60;
            if (minutes < 60) return minutes + "m";
            return string.Format("{0}h {1:00}m", minutes / 60, minutes % 60);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_app.IsShuttingDown)
            {
                e.Cancel = true;
                _app.HideMain();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Oemcomma)
            {
                _app.ShowSettings();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (Program.ShowMessage != 0 && m.Msg == Program.ShowMessage)
            {
                _app.ShowMain();
                return;
            }
            base.WndProc(ref m);
        }
    }

    internal class StatusChip : Control
    {
        private Color _dotColor = Theme.Accent;
        private string _text = "专注中";

        public Color DotColor
        {
            get { return _dotColor; }
            set { _dotColor = value; Invalidate(); }
        }

        public override string Text
        {
            get { return _text; }
            set { _text = value; Invalidate(); }
        }

        public StatusChip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.CreateFont(8.5f, FontStyle.Regular);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            using (GraphicsPath path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Height / 2f))
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

            int cx = 14;
            using (SolidBrush dot = new SolidBrush(_dotColor))
            {
                g.FillEllipse(dot, cx - 3, Height / 2f - 3, 6, 6);
            }

            Rectangle textRect = new Rectangle(24, 0, Width - 32, Height);
            TextRenderer.DrawText(g, _text, Font, textRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    internal class StatCard : Control
    {
        private string _title = "";
        private string _value = "";
        private Color _valueColor = Theme.Text;

        public string TitleText
        {
            get { return _title; }
            set { _title = value; Invalidate(); }
        }

        public string ValueText
        {
            get { return _value; }
            set { _value = value; Invalidate(); }
        }

        public Color ValueColor
        {
            get { return _valueColor; }
            set { _valueColor = value; Invalidate(); }
        }

        public StatCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            using (GraphicsPath path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 12f))
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

            using (Font small = Theme.CreateFont(Font.FontFamily.Name, 8.5f, FontStyle.Regular))
            using (Font big = Theme.CreateFont(Font.FontFamily.Name, 14f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _title, small, new Rectangle(16, 14, Width - 32, 20), Theme.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, _value, big, new Rectangle(16, 36, Width - 32, 28), _valueColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
