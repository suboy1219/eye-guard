using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace EyeGuard
{
    public class Config
    {
        public int WorkMinutes = 45;
        public int RestMinutes = 5;
        public int BlueLight = 40;
        public bool MicroBreak = false;
        public int MicroMinutes = 20;
        public int MicroSeconds = 20;
        public bool ShowOverlay = true;
        public bool AllowSkip = true;
        public bool AutoStart = false;
        public bool NotifyBefore = true;
        public int TodayRests = 0;
        public string Today = "";
        public int TodayWorkSeconds = 0;

        public static string DefaultDirectory
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "EyeGuard");
            }
        }

        public static string DefaultPath
        {
            get { return Path.Combine(DefaultDirectory, "config.ini"); }
        }

        public static Config Load()
        {
            return Load(DefaultPath);
        }

        public static Config Load(string path)
        {
            Config cfg = new Config();
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(path))
                {
                    foreach (string raw in File.ReadAllLines(path))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                }
            }
            catch
            {
                return cfg;
            }

            FieldInfo[] fields = typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                string text;
                if (!values.TryGetValue(field.Name, out text)) continue;
                try
                {
                    if (field.FieldType == typeof(int))
                    {
                        int number;
                        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                        {
                            field.SetValue(cfg, number);
                        }
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        field.SetValue(cfg, text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        field.SetValue(cfg, text);
                    }
                }
                catch
                {
                }
            }

            cfg.Sanitize();
            cfg.RollDay();
            return cfg;
        }

        public void Save()
        {
            Save(DefaultPath);
        }

        public void Save(string path)
        {
            Sanitize();
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                FieldInfo[] fields = typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Instance);
                List<string> lines = new List<string>();
                lines.Add("# EyeGuard settings");
                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(this);
                    string text;
                    if (value is bool) text = ((bool)value) ? "1" : "0";
                    else text = Convert.ToString(value, CultureInfo.InvariantCulture);
                    lines.Add(field.Name + "=" + text);
                }
                File.WriteAllLines(path, lines.ToArray());
            }
            catch
            {
            }
        }

        public Config Clone()
        {
            return (Config)MemberwiseClone();
        }

        public void Sanitize()
        {
            WorkMinutes = Clamp(WorkMinutes, 1, 180);
            RestMinutes = Clamp(RestMinutes, 1, 60);
            BlueLight = Clamp(BlueLight, 0, 100);
            MicroMinutes = Clamp(MicroMinutes, 1, 120);
            MicroSeconds = Clamp(MicroSeconds, 5, 120);
            TodayWorkSeconds = Math.Max(0, TodayWorkSeconds);
            TodayRests = Math.Max(0, TodayRests);
        }

        public void RollDay()
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!string.Equals(Today, now, StringComparison.Ordinal))
            {
                Today = now;
                TodayRests = 0;
                TodayWorkSeconds = 0;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
