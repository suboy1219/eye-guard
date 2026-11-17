using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EyeGuard
{
    internal static class Program
    {
        public static readonly int ShowMessage = Native.RegisterWindowMessage("EyeGuard.ShowMain.v1");

        [STAThread]
        private static void Main(string[] args)
        {
            if (HasArg(args, "--selftest"))
            {
                RunSelfTest();
                return;
            }

            if (HasArg(args, "--reset"))
            {
                Gamma.Restore();
                return;
            }

            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool created;
            using (Mutex mutex = new Mutex(true, "EyeGuard.SingleInstance.v1", out created))
            {
                if (!created)
                {
                    if (ShowMessage != 0)
                    {
                        Native.PostMessage((IntPtr)Native.HWND_BROADCAST, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    Gamma.Restore();
                    MessageBox.Show("护眼卫士遇到问题：" + e.Exception.Message, "护眼卫士",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate
                {
                    Gamma.Restore();
                };
                AppDomain.CurrentDomain.ProcessExit += delegate
                {
                    Gamma.Restore();
                };

                try
                {
                    Application.Run(new EyeGuardContext(
                        HasArg(args, "--tray"),
                        HasArg(args, "--demo-rest"),
                        HasArg(args, "--demo-settings")));
                }
                finally
                {
                    Gamma.Restore();
                }
            }
        }

        private static bool HasArg(string[] args, string name)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static void RunSelfTest()
        {
            List<string> lines = new List<string>();
            bool ok = true;
            string tempDir = Path.Combine(Path.GetTempPath(), "EyeGuardSelfTest");

            try
            {
                Directory.CreateDirectory(tempDir);
                string path = Path.Combine(tempDir, "config.ini");
                if (File.Exists(path)) File.Delete(path);

                Config cfg = Config.Load(path);
                cfg.WorkMinutes = 37;
                cfg.RestMinutes = 7;
                cfg.BlueLight = 25;
                cfg.AutoStart = true;
                cfg.Save(path);

                Config loaded = Config.Load(path);
                bool configOk = loaded.WorkMinutes == 37 && loaded.RestMinutes == 7 &&
                                loaded.BlueLight == 25 && loaded.AutoStart;
                ok = ok && configOk;
                lines.Add("config.readwrite=" + (configOk ? "ok" : "fail"));

                bool gammaOk = Gamma.Apply(0.35);
                bool restored = Gamma.Restore();
                ok = ok && restored;
                lines.Add("gamma.apply=" + (gammaOk ? "ok" : "unavailable"));
                lines.Add("gamma.restore=" + (restored ? "ok" : "fail"));

                lines.Add("screens=" + Screen.AllScreens.Length);
                lines.Add("exe=" + Application.ExecutablePath);
                lines.Add("surface=" + (Environment.OSVersion.VersionString));

                string invalid = Path.Combine(tempDir, "bad.ini");
                File.WriteAllText(invalid, "WorkMinutes=abc\nBlueLight=999\nRestMinutes=-4\n");
                Config bad = Config.Load(invalid);
                bool clampOk = bad.BlueLight == 100 && bad.RestMinutes == 1 && bad.WorkMinutes == 45;
                ok = ok && clampOk;
                lines.Add("config.sanitize=" + (clampOk ? "ok" : "fail"));

                File.Delete(path);
                File.Delete(invalid);
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                ok = false;
                lines.Add("exception=" + ex.Message);
                Gamma.Restore();
            }

            lines.Add("result=" + (ok ? "PASS" : "FAIL"));
            string report = string.Join(Environment.NewLine, lines.ToArray());

            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "eyeguard-selftest.log"), report, Encoding.UTF8);
            }
            catch
            {
            }

            Console.WriteLine(report);
            Environment.ExitCode = ok ? 0 : 1;
        }
    }
}
