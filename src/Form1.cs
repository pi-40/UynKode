using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;

namespace UynkodeInterpreter
{
    public partial class AppForm : Form
    {
        public string TargetScriptPath { get; set; } = string.Empty;
        private readonly Dictionary<string, Action<string>> _commandRegistry = new Dictionary<string, Action<string>>();
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        private readonly List<Form> _spawnedWindows = new List<Form>();
        private static Form _gdiLayer = null;

        [DllImport("user32.dll")] private static extern bool BlockInput(bool fBlockIt);
        [DllImport("user32.dll")] private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public AppForm()
        {
            InitializeComponent();
            BuildMassiveCommandMatrix();
        }

        private void AppForm_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            if (!string.IsNullOrEmpty(TargetScriptPath)) RunUk1Script(TargetScriptPath);
            Application.Exit();
        }

        private void BuildMassiveCommandMatrix()
        {
            // --- CORE ORIGINAL COMMANDS (WITH GDI ENHANCEMENTS) ---
            _commandRegistry.Add("peui3", SetBackgroundColor);
            _commandRegistry.Add("scrm", PrintText);
            _commandRegistry.Add("fnd", LaunchFile);
            _commandRegistry.Add("xtrnl", CreateUiBlockGdi); // Re-routed to our new raw GDI handler
            _commandRegistry.Add("toast", PlayNativeAudio);

            // --- MATRIX STRUCTURAL COMMAND LOOPS (500 INTERFACES) ---
            for (int i = 1; i <= 50; i++)
            {
                int id = i;
                _commandRegistry.Add($"vset{id}", arg => _variables[$"var_{id}"] = Clean(arg));
                _commandRegistry.Add($"vget{id}", arg => MessageBox.Show(_variables.ContainsKey($"var_{id}") ? _variables[$"var_{id}"] : "Empty"));
                _commandRegistry.Add($"vclr{id}", arg => _variables.Remove($"var_{id}"));
            }
            for (int i = 51; i <= 100; i++)
            {
                int step = i - 50;
                _commandRegistry.Add($"winopac{step}", arg => this.Opacity = step / 50.0);
                _commandRegistry.Add($"winflash{step}", arg => { if (_gdiLayer != null) _gdiLayer.BackColor = (step % 2 == 0) ? Color.Red : Color.LimeGreen; });
            }
            for (int i = 101; i <= 150; i++)
            {
                int ch = i - 100;
                _commandRegistry.Add($"audiochan{ch}", arg => PlayNativeAudio(arg));
                _commandRegistry.Add($"audiomute{ch}", arg => { var p = new System.Media.SoundPlayer(); p.Stop(); });
                _commandRegistry.Add($"beepfreq{ch}", arg => { if (int.TryParse(Clean(arg), out int f)) Console.Beep(f, 150); });
            }
            for (int i = 151; i <= 200; i++)
            {
                int code = i;
                _commandRegistry.Add($"syskill{code}", arg => Process.GetCurrentProcess().Kill());
                _commandRegistry.Add($"sysrun{code}", arg => Process.Start("cmd.exe", $"/c {Clean(arg)}"));
            }
            for (int i = 201; i <= 250; i++)
            {
                int fIndex = i - 200;
                _commandRegistry.Add($"fmake{fIndex}", arg => File.WriteAllText(Clean(arg), "Uynkode Generated Data"));
                _commandRegistry.Add($"fdel{fIndex}", arg => { if (File.Exists(Clean(arg))) File.Delete(Clean(arg)); });
            }
            for (int i = 251; i <= 300; i++)
            {
                int node = i - 250;
                _commandRegistry.Add($"canvas{node}", arg => CreateUiBlockGdi(arg));
                _commandRegistry.Add($"canvasclr{node}", arg => ClearGdiLayers());
            }
            for (int i = 301; i <= 350; i++)
            {
                int netId = i - 300;
                _commandRegistry.Add($"neturl{netId}", arg => Process.Start(new ProcessStartInfo(Clean(arg)) { UseShellExecute = true }));
            }
            for (int i = 351; i <= 400; i++)
            {
                int msgId = i - 350;
                _commandRegistry.Add($"msgbox{msgId}", arg => MessageBox.Show(Clean(arg), "Uynkode Engine Alert"));
                _commandRegistry.Add($"msgerr{msgId}", arg => MessageBox.Show(Clean(arg), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            for (int i = 401; i <= 450; i++)
            {
                int inputId = i - 400;
                _commandRegistry.Add($"mousetog{inputId}", arg => Cursor.Position = new Point(Cursor.Position.X + inputId, Cursor.Position.Y + inputId));
                _commandRegistry.Add($"sendkey{inputId}", arg => SendKeys.SendWait(Clean(arg)));
            }
            for (int i = 451; i <= 500; i++)
            {
                int utilId = i - 450;
                _commandRegistry.Add($"sleepms{utilId}", arg => Thread.Sleep(utilId * 10));
                _commandRegistry.Add($"debuglog{utilId}", arg => Debug.WriteLine($"Matrix Trace Point: {utilId}"));
            }
        }

        public void RunUk1Script(string filePath)
        {
            if (!File.Exists(filePath)) return;
            string[] lines = File.ReadAllLines(filePath);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

                int firstSpace = line.IndexOf(' ');
                string command = firstSpace == -1 ? line.ToLower() : line.Substring(0, firstSpace).ToLower();
                string arguments = firstSpace == -1 ? string.Empty : line.Substring(firstSpace + 1).Trim();

                if (_commandRegistry.ContainsKey(command))
                {
                    try { _commandRegistry[command].Invoke(arguments); } catch { }
                }
            }
        }

        #region Engine GDI Graphic Implementations
        private string Clean(string input) => input.Replace("``", "").Trim();

        private void CreateUiBlockGdi(string axisArgs)
        {
            // Parses formatting fields: ``X Y Z axis
            string rawArgs = axisArgs.Replace("``", "").Replace("axis", "").Trim();
            string[] segments = rawArgs.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 3)
            {
                if (int.TryParse(segments[0], out int x) &&
                    int.TryParse(segments[1], out int y) &&
                    int.TryParse(segments[2], out int z))
                {
                    // Ensure our global top-level drawing canvas is active
                    if (_gdiLayer == null || _gdiLayer.IsDisposed)
                    {
                        _gdiLayer = new Form
                        {
                            FormBorderStyle = FormBorderStyle.None,
                            Bounds = Screen.PrimaryScreen.Bounds,
                            StartPosition = FormStartPosition.Manual,
                            TopMost = true,
                            ShowInTaskbar = false,
                            BackColor = Color.Lime, // Chroma transparency key
                            TransparencyKey = Color.Lime
                        };
                        _gdiLayer.Show();
                    }

                    // Use native GDI+ to render graphics over the screen context
                    using (Graphics g = _gdiLayer.CreateGraphics())
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        // Z maps directly out to define structural box sizing limits dynamically
                        int sizeWidthHeight = z <= 0 ? 60 : z;

                        // Render a neon tech border block frame using basic math properties
                        using (Pen neonPen = new Pen(Color.Cyan, 3f))
                        {
                            g.DrawRectangle(neonPen, x, y, sizeWidthHeight, sizeWidthHeight);
                        }

                        // Inner solid visual node Core
                        using (SolidBrush alphaBrush = new SolidBrush(Color.FromArgb(180, 255, 0, 128)))
                        {
                            g.FillRectangle(alphaBrush, x + 4, y + 4, sizeWidthHeight - 8, sizeWidthHeight - 8);
                        }
                    }
                }
            }
        }

        private void ClearGdiLayers()
        {
            if (_gdiLayer != null && !_gdiLayer.IsDisposed)
            {
                _gdiLayer.Invalidate(); // Repaint cleans dirty canvas structures instantly
                _gdiLayer.Close();
                _gdiLayer = null;
            }
        }

        private void SetBackgroundColor(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            this.BackColor = ColorTranslator.FromHtml(hex);
        }

        private void PrintText(string args) => MessageBox.Show(Clean(args), "Uynkode System Output");

        private void LaunchFile(string path)
        {
            string p = Clean(path);
            if (File.Exists(p) || Directory.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true });
        }

        private void PlayNativeAudio(string audioPath)
        {
            string p = Clean(audioPath);
            if (File.Exists(p) && p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var pl = new System.Media.SoundPlayer(p);
                pl.Play();
            }
        }
        #endregion
    }
}