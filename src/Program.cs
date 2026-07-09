using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UynkodeInterpreter
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(appDir, "app.ico");

            if (File.Exists(iconPath))
            {
                try { RegisterFileAssociation(".uk1", "UynkodeScript", "Uynkode Engine File", iconPath); } catch { }
            }
            else
            {
                MessageBox.Show("This program is missing files. This program will not work correctly",
                                "System Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            AppForm mainForm = new AppForm();
            if (args.Length > 0) mainForm.TargetScriptPath = args[0];

            Application.Run(mainForm);
        }

        private static void RegisterFileAssociation(string extension, string className, string description, string iconPath)
        {
            string exePath = Application.ExecutablePath;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
            {
                if (key == null) return;
                using (RegistryKey extKey = key.CreateSubKey(extension)) extKey.SetValue("", className);
                using (RegistryKey clsKey = key.CreateSubKey(className))
                {
                    clsKey.SetValue("", description);
                    using (RegistryKey iconKey = clsKey.CreateSubKey("DefaultIcon")) iconKey.SetValue("", $"\"{iconPath}\",0");
                    using (RegistryKey shellKey = clsKey.CreateSubKey(@"shell\open\command")) shellKey.SetValue("", $"\"{exePath}\" \"%1\"");
                }
            }
        }
    }
}