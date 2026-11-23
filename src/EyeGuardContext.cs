using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EyeGuard
{
    public enum RunState
    {
        Working,
        Resting,
        Paused
    }

    public class EyeGuardContext : ApplicationContext
    {
        public Config Cfg;
        public RunState State;
        public int WorkLeft;
        public int RestLeft;
        public int RestTotal;
        public bool IsShuttingDown;

        private readonly MainForm _main;
        private readonly NotifyIcon _tray;
        private readonly Timer _timer;
        private readonly List<OverlayForm> _overlays = new List<OverlayForm>();
        private readonly ToolStripMenuItem _pauseItem;
        private Icon _iconWork;
        private Icon _iconRest;
        private Icon _iconPause;
        private RunState _iconState;
        private ToastForm _toast;
        private DateTime _lastTick;
        private double _accumulator;
        private double _saveAccumulator;
        private bool _warned;
        private bool _hideHintShown;
        private int _microLeft;

        public EyeGuardContext(bool startHidden, bool demoRest, bool demoSettings)
        {
            Cfg = Config.Load();
            Cfg.RollDay();
            WorkLeft = Cfg.WorkMinutes * 60;
            RestTotal = Cfg.RestMinutes * 60;
            RestLeft = RestTotal;
            _microLeft = Cfg.MicroMinutes * 60;
            State = RunState.Working;
            _lastTick = DateTime.Now;
            _iconState = (RunState)(-1);

            Gamma.Apply(Cfg.BlueLight / 100.0);
            ApplyAutoStart();

            _main = new MainForm(this);
            IntPtr forced = _main.Handle;
            _main.UpdateView();

            _iconWork = Theme.CreateStateIcon(Theme.Accent, Color.White);
            _iconRest = Theme.CreateStateIcon(Theme.Warm, Color.White);
            _iconPause = Theme.CreateStateIcon(Theme.Muted, Color.White);

            _pauseItem = new ToolStripMenuItem("暂停");
            _pauseItem.Click += delegate { TogglePause(); };

            _tray = new NotifyIcon();
            _tray.Icon = _iconWork;
            _tray.Text = "护眼卫士";
            _tray.Visible = true;
            _tray.DoubleClick += delegate(object sender, EventArgs e) { ShowMain(); };
            _tray.ContextMenuStrip = BuildMenu();

            _timer = new Timer();
            _timer.Interval = 500;
            _timer.Tick += OnTick;
            _timer.Start();

            if (startHidden)
            {
                ShowToast("护眼卫士已在后台守护", "每 " + Cfg.WorkMinutes + " 分钟提醒你休息一次。", Theme.Accent);
            }
            else
            {
                _main.Show();
                _main.Activate();
            }

            if (demoRest)
            {
                Timer demo = new Timer();
                demo.Interval = 1200;
                demo.Tick += delegate
                {
                    demo.Stop();
                    demo.Dispose();
                    StartRest(true);
                };
                demo.Start();
            }

            if (demoSettings)
            {
                Timer demoSettingsTimer = new Timer();
                demoSettingsTimer.Interval = 1200;
                demoSettingsTimer.Tick += delegate
                {
                    demoSettingsTimer.Stop();
                    demoSettingsTimer.Dispose();
                    ShowSettings();
                };
                demoSettingsTimer.Start();
            }

            UpdateAll();
        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Renderer = new DarkMenuRenderer();
            menu.ShowImageMargin = false;
            menu.BackColor = Theme.Surface;
            menu.ForeColor = Theme.Text;
            menu.Font = Theme.CreateFont(9f, FontStyle.Regular);
            menu.Padding = new Padding(0, 4, 0, 4);

            ToolStripMenuItem show = new ToolStripMenuItem("打开面板");
            show.Click += delegate { ShowMain(); };
            menu.Items.Add(show);

            ToolStripMenuItem rest = new ToolStripMenuItem("立即休息");
            rest.Click += delegate { ToggleRest(); };
            menu.Items.Add(rest);
            menu.Items.Add(_pauseItem);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem settings = new ToolStripMenuItem("设置…");
            settings.Click += delegate { ShowSettings(); };
            menu.Items.Add(settings);

            ToolStripMenuItem reapply = new ToolStripMenuItem("重置屏幕色彩");
            reapply.Click += delegate
            {
                Gamma.Apply(Cfg.BlueLight / 100.0);
                ShowToast("已重新应用", Cfg.BlueLight > 0 ? "蓝光过滤 " + Cfg.BlueLight + "%" : "屏幕色彩已恢复", Theme.Accent);
            };
            menu.Items.Add(reapply);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(UpdateService.CreateHelpMenu());
            ToolStripMenuItem exit = new ToolStripMenuItem("退出护眼卫士");
            exit.Click += delegate { ExitApp(); };
            menu.Items.Add(exit);
            return menu;
        }

        private void OnTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double elapsed = (now - _lastTick).TotalSeconds;
            _lastTick = now;
            if (elapsed < 0) elapsed = 0;
            if (elapsed > 3) elapsed = 3;
            _accumulator += elapsed;

            int whole = (int)_accumulator;
            if (whole > 0)
            {
                _accumulator -= whole;
                Advance(whole);
            }

            UpdateAll();

            _saveAccumulator += elapsed;
            if (_saveAccumulator >= 60)
            {
                _saveAccumulator = 0;
                Cfg.Save();
            }
        }

        private void Advance(int seconds)
        {
            Cfg.RollDay();

            if (State == RunState.Working)
            {
                WorkLeft -= seconds;
                Cfg.TodayWorkSeconds += seconds;

                if (Cfg.MicroBreak)
                {
                    _microLeft -= seconds;
                    if (_microLeft <= 0)
                    {
                        _microLeft = Cfg.MicroMinutes * 60;
                        ShowToast("远眺 20 秒", "把视线移到 6 米外，顺便活动一下肩颈。", Theme.Accent);
                    }
                }

                if (Cfg.NotifyBefore && !_warned && WorkLeft <= 30)
                {
                    _warned = true;
                    ShowToast("准备休息", "还有 30 秒到休息时间，先把手头这一步收个尾。", Theme.Warm);
                }

                if (WorkLeft <= 0)
                {
                    StartRest(false);
                }
            }
            else if (State == RunState.Resting)
            {
                RestLeft -= seconds;
                UpdateOverlays();
                if (RestLeft <= 0)
                {
                    EndRest(true);
                }
            }
        }

        private void UpdateAll()
        {
            if (IsShuttingDown) return;

            if (_main != null && !_main.IsDisposed)
            {
                _main.UpdateView();
            }

            UpdateTrayIcon();

            if (_pauseItem != null)
            {
                _pauseItem.Text = State == RunState.Paused ? "继续" : "暂停";
            }

            if (_tray != null)
            {
                int seconds = State == RunState.Resting ? RestLeft : WorkLeft;
                string prefix = State == RunState.Resting ? "休息" : (State == RunState.Paused ? "已暂停" : "工作");
                _tray.Text = string.Format("护眼卫士 · {0} {1:00}:{2:00}", prefix, Math.Max(0, seconds) / 60, Math.Max(0, seconds) % 60);
            }
        }

        private void UpdateTrayIcon()
        {
            if (_tray == null || _iconState == State) return;
            _iconState = State;
            if (State == RunState.Resting) _tray.Icon = _iconRest;
            else if (State == RunState.Paused) _tray.Icon = _iconPause;
            else _tray.Icon = _iconWork;
        }

        public void ToggleRest()
        {
            if (State == RunState.Resting)
            {
                EndRest(false);
            }
            else
            {
                StartRest(true);
            }
        }

        private void StartRest(bool manual)
        {
            if (State == RunState.Resting) return;
            State = RunState.Resting;
            RestTotal = Cfg.RestMinutes * 60;
            RestLeft = RestTotal;
            Cfg.TodayRests++;
            _warned = false;
            _microLeft = Cfg.MicroMinutes * 60;

            RestMessage message = RestMessage.Next();
            if (Cfg.ShowOverlay)
            {
                ShowOverlays(message);
            }
            else
            {
                ShowToast(message.Title, string.Format("离开屏幕 {0} 分钟，眺望远处放松眼睛。", Cfg.RestMinutes), Theme.Warm);
            }

            UpdateAll();
            Cfg.Save();
        }

        public void EndRest(bool natural)
        {
            if (State != RunState.Resting) return;
            CloseOverlays();
            State = RunState.Working;
            WorkLeft = Cfg.WorkMinutes * 60;
            _warned = false;
            _microLeft = Cfg.MicroMinutes * 60;

            if (natural)
            {
                ShowToast("休息结束", "继续工作吧，记得多眨眼、保持坐姿。", Theme.Accent);
            }

            UpdateAll();
            Cfg.Save();
        }

        public void TogglePause()
        {
            if (State == RunState.Resting)
            {
                EndRest(false);
                return;
            }

            if (State == RunState.Paused)
            {
                State = RunState.Working;
                _warned = false;
                ShowToast("继续计时", "已经恢复专注计时。", Theme.Accent);
            }
            else
            {
                State = RunState.Paused;
            }

            UpdateAll();
            Cfg.Save();
        }

        private void ShowOverlays(RestMessage message)
        {
            CloseOverlays();
            Screen[] screens = Screen.AllScreens;

            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].Primary) continue;
                AddOverlay(screens[i], false, message);
            }
            for (int i = 0; i < screens.Length; i++)
            {
                if (!screens[i].Primary) continue;
                AddOverlay(screens[i], true, message);
            }

            UpdateOverlays();
        }

        private void AddOverlay(Screen screen, bool primary, RestMessage message)
        {
            OverlayForm overlay = new OverlayForm(screen, primary, Cfg.AllowSkip, message);
            overlay.SkipRequested += delegate
            {
                if (_main != null && _main.IsHandleCreated)
                {
                    _main.BeginInvoke(new Action(delegate { EndRest(false); }));
                }
            };
            _overlays.Add(overlay);
            overlay.Show();
            if (primary)
            {
                overlay.Activate();
            }
        }

        private void UpdateOverlays()
        {
            for (int i = 0; i < _overlays.Count; i++)
            {
                if (_overlays[i] != null && !_overlays[i].IsDisposed)
                {
                    _overlays[i].SetRest(RestLeft, RestTotal);
                }
            }
        }

        private void CloseOverlays()
        {
            for (int i = _overlays.Count - 1; i >= 0; i--)
            {
                OverlayForm overlay = _overlays[i];
                _overlays.RemoveAt(i);
                if (overlay == null || overlay.IsDisposed) continue;
                overlay.AllowClose();
                overlay.Hide();
                overlay.Close();
                overlay.Dispose();
            }
        }

        public void ShowMain()
        {
            if (_main == null || _main.IsDisposed) return;
            if (!_main.Visible) _main.Show();
            if (_main.WindowState == FormWindowState.Minimized || _main.WindowState == FormWindowState.Maximized)
            {
                _main.WindowState = FormWindowState.Normal;
            }
            _main.Activate();
            _main.BringToFront();
            Native.SetForegroundWindow(_main.Handle);
        }

        public void HideMain()
        {
            if (_main == null || _main.IsDisposed) return;
            _main.Hide();
            if (!_hideHintShown)
            {
                _hideHintShown = true;
                ShowToast("仍在后台守护", "双击托盘图标可重新打开，右键可退出。", Theme.Accent);
            }
        }

        public void ShowSettings()
        {
            ShowMain();
            Config draft = Cfg.Clone();
            using (SettingsForm form = new SettingsForm(draft, this))
            {
                if (form.ShowDialog(_main) == DialogResult.OK)
                {
                    ApplySettings(draft);
                }
                else
                {
                    Gamma.Apply(Cfg.BlueLight / 100.0);
                }
            }
        }

        private void ApplySettings(Config draft)
        {
            Cfg = draft;
            Cfg.Sanitize();
            Cfg.RollDay();
            Cfg.Save();

            Gamma.Apply(Cfg.BlueLight / 100.0);
            ApplyAutoStart();

            if (State == RunState.Working)
            {
                WorkLeft = Math.Min(WorkLeft, Cfg.WorkMinutes * 60);
            }
            RestTotal = Cfg.RestMinutes * 60;
            _microLeft = Math.Min(_microLeft, Cfg.MicroMinutes * 60);

            ShowToast("设置已保存", Cfg.ShowOverlay ? "休息时会显示全屏遮罩。" : "休息时只在角落提示。", Theme.Accent);
            UpdateAll();
        }

        public void PreviewBlueLight(int percent)
        {
            Gamma.Apply(percent / 100.0);
        }

        private void ApplyAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (Cfg.AutoStart)
                    {
                        key.SetValue("EyeGuard", "\"" + Application.ExecutablePath + "\" --tray");
                    }
                    else
                    {
                        key.DeleteValue("EyeGuard", false);
                    }
                }
            }
            catch
            {
            }
        }

        private void ShowToast(string title, string message, Color accent)
        {
            try
            {
                if (_toast != null && !_toast.IsDisposed)
                {
                    _toast.Close();
                    _toast.Dispose();
                }
                _toast = new ToastForm(title, message, _iconWork, accent);
                _toast.Show();
            }
            catch
            {
            }
        }

        public void ExitApp()
        {
            if (IsShuttingDown) return;
            IsShuttingDown = true;

            try { _timer.Stop(); } catch { }
            CloseOverlays();
            Cfg.Save();
            Gamma.Restore();

            if (_toast != null && !_toast.IsDisposed)
            {
                _toast.Close();
                _toast.Dispose();
            }
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            if (_main != null && !_main.IsDisposed)
            {
                _main.Close();
                _main.Dispose();
            }

            DisposeIcons();
            ExitThread();
        }

        private void DisposeIcons()
        {
            if (_iconWork != null) { _iconWork.Dispose(); _iconWork = null; }
            if (_iconRest != null) { _iconRest.Dispose(); _iconRest = null; }
            if (_iconPause != null) { _iconPause.Dispose(); _iconPause = null; }
        }
    }
}
