using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace EyeGuard
{
    internal static class UpdateService
    {
        internal const string ProjectUrl = "https://github.com/suboy1219/eye-guard";
        internal static readonly Version CurrentVersion = typeof(UpdateService).Assembly.GetName().Version;
        private static bool _checking;

        internal static ToolStripMenuItem CreateHelpMenu()
        {
            ToolStripMenuItem help = new ToolStripMenuItem("帮助");
            ToolStripMenuItem check = new ToolStripMenuItem("检测更新");
            help.DropDownOpening += delegate
            {
                if (help.Owner != null)
                {
                    help.DropDown.Renderer = help.Owner.Renderer;
                    help.DropDown.BackColor = help.Owner.BackColor;
                    help.DropDown.ForeColor = help.Owner.ForeColor;
                    foreach (ToolStripItem item in help.DropDownItems) item.ForeColor = help.Owner.ForeColor;
                }
                check.Enabled = !_checking;
                check.Text = _checking ? "正在检测更新…" : "检测更新";
            };
            check.Click += async delegate
            {
                if (_checking) return;
                _checking = true;
                check.Enabled = false;
                check.Text = "正在检测更新…";
                try
                {
                    string tag = await Task.Run<string>(() => GetLatestTag());
                    Version latest;
                    if (tag == null)
                        MessageBox.Show("暂时没有可用的正式发布版。", "检测更新",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else if (!TryParseVersion(tag, out latest))
                        throw new FormatException("无法识别发布版本号。");
                    else if (latest > CurrentVersion)
                    {
                        if (MessageBox.Show("发现新版本 " + tag + "（当前 " + CurrentVersion.ToString(3) +
                            "）。\n\n是否前往 GitHub 发布页下载？", "检测更新", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                            Open(ProjectUrl + "/releases/tag/" + Uri.EscapeDataString(tag));
                    }
                    else
                        MessageBox.Show("当前已是最新版本（" + CurrentVersion.ToString(3) + "）。", "检测更新",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    if (!(ex is WebException || ex is IOException || ex is FormatException ||
                        ex is ArgumentException || ex is InvalidOperationException)) throw;
                    MessageBox.Show("无法检测更新，请检查网络连接后重试。GitHub 暂时不可用或请求次数受限时也可能出现此提示。",
                        "检测更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    _checking = false;
                    if (!check.IsDisposed)
                    {
                        check.Enabled = true;
                        check.Text = "检测更新";
                    }
                }
            };
            help.DropDownItems.Add("使用说明", null, delegate { Open(ProjectUrl + "#readme"); });
            help.DropDownItems.Add(check);
            help.DropDownItems.Add("关于", null, delegate
            {
                MessageBox.Show("护眼卫士 " + CurrentVersion.ToString(3) + "\n\n" + ProjectUrl,
                    "关于护眼卫士", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            return help;
        }

        private static string GetLatestTag()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                "https://api.github.com/repos/suboy1219/eye-guard/releases/latest");
            request.UserAgent = "EyeGuard/" + CurrentVersion.ToString(3);
            request.Accept = "application/vnd.github+json";
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            try
            {
                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    return ParseRelease(reader.ReadToEnd());
            }
            catch (WebException ex)
            {
                using (HttpWebResponse response = ex.Response as HttpWebResponse)
                {
                    if (response != null && response.StatusCode == HttpStatusCode.NotFound) return null;
                }
                throw;
            }
        }

        internal static string ParseRelease(string json)
        {
            Dictionary<string, object> release = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            if (release == null) throw new FormatException("更新信息为空。");
            object draft, prerelease, tag;
            if ((release.TryGetValue("draft", out draft) && object.Equals(draft, true)) ||
                (release.TryGetValue("prerelease", out prerelease) && object.Equals(prerelease, true))) return null;
            Version version;
            if (!release.TryGetValue("tag_name", out tag) || !(tag is string) ||
                !TryParseVersion((string)tag, out version)) throw new FormatException("无法识别发布版本号。");
            return ((string)tag).Trim();
        }

        internal static bool TryParseVersion(string value, out Version version)
        {
            string number = value.Trim().TrimStart('v', 'V').Split('+')[0];
            if (!Version.TryParse(number, out version)) return false;
            version = new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
            return true;
        }

        private static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                if (!(ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)) throw;
                MessageBox.Show("无法打开浏览器，请手动访问：\n" + url, "护眼卫士",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
