using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace LimitNvpstate {
    public partial class LimitNvpstate : Form {
        private readonly Process _inspector = new Process();

        public LimitNvpstate() {
            InitializeComponent();

            Text = $"limit-nvpstate v{Program.Version.Major}.{Program.Version.Minor}.{Program.Version.Build}";
        }

        private void AddProcessClick(object sender, EventArgs e) {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) {
                var fileName = Path.GetFileName(ofd.FileName).Replace(".exe", "");
                _ = processes.Items.Add(fileName);
            }
        }

        private void RemoveProcessClick(object sender, EventArgs e) {
            processes.Items.RemoveAt(processes.SelectedIndex);
        }

        private void EventHandler(object sender, EventArrivedEventArgs e) {
            var createdProcess = Process.GetProcessById((int)(uint)e.NewEvent.Properties["ProcessID"].Value);

            if (processes.Items.Contains(createdProcess.ProcessName)) {
                LimitPstate(false);
                createdProcess.WaitForExit();
                LimitPstate(true);
            }
        }

        private void LoadSettings() {
            using (var config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {

                if (config.GetValue("LimitPState") == null) {
                    config.SetValue("LimitPState", "1", RegistryValueKind.String);
                }

                if (config.GetValue("IndexOfGPU") == null) {
                    config.SetValue("IndexOfGPU", "0", RegistryValueKind.String);
                }

                if (config.GetValue("StartMinimized") == null) {
                    config.SetValue("StartMinimized", "False", RegistryValueKind.String);
                }

                if (config.GetValue("ProcessList") == null) {
                    config.SetValue("ProcessList", new string[] { }, RegistryValueKind.MultiString);
                }

                pstateLimit.SelectedIndex = Convert.ToInt32(config.GetValue("LimitPState"));
                gpuIndex.SelectedIndex = Convert.ToInt32(config.GetValue("IndexOfGPU"));
                startMinimizedToolStripMenuItem.Checked = Convert.ToBoolean(config.GetValue("StartMinimized"));

                processes.Items.Clear();
                foreach (var i in (string[])config.GetValue("ProcessList")) {
                    _ = processes.Items.Add(i);
                }
            }
        }

        private void LimitPstate(bool enabled) {
            _inspector.StartInfo.Arguments = enabled
                ? $"-setPStateLimit:{gpuIndex.SelectedIndex},{pstateLimit.SelectedItem.ToString().Replace("P", "")}"
                : $"-setPStateLimit:{gpuIndex.SelectedIndex},0";
            _ = _inspector.Start();
        }

        private void ApplySettingsClick(object sender, EventArgs e) {
            using (var config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {
                config.SetValue("LimitPState", pstateLimit.SelectedIndex, RegistryValueKind.String);
                config.SetValue("IndexOfGPU", gpuIndex.SelectedIndex, RegistryValueKind.String);
                config.SetValue("ProcessList", processes.Items.OfType<string>().ToArray(), RegistryValueKind.MultiString);
            }
            LoadSettings();
        }

        private void LimitNvpstateLoad(object sender, EventArgs e) {
            // create a new instance of the ManagementObjectSearcher class
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            // call the Get method of the ManagementObjectSearcher class to retrieve a collection of GPU objects
            var gpuCollection = searcher.Get();

            // iterate through the collection of GPU objects
            foreach (var gpu in gpuCollection.Cast<ManagementObject>()) {
                _ = gpuIndex.Items.Add($"{gpu["Name"]}");
            }

            if (!File.Exists("nvidiaInspector.exe")) {
                _ = MessageBox.Show("Inspector not found in current directory", "limit-nvpstate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                FormClosing -= LimitnvpstateFormClosing;
                Close();
            }

            // configure inspector launch settings
            _inspector.StartInfo.FileName = "nvidiaInspector.exe";
            _inspector.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // configure event handler
            var startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived += new EventArrivedEventHandler(EventHandler);
            startWatch.Start();


            LoadSettings(); // load settings when program starts
            LimitPstate(true); // limit pstates when program starts

            if (startMinimizedToolStripMenuItem.Checked) {
                WindowState = FormWindowState.Minimized;
            }

            removeProcess.Enabled = false; // disable remove button by default, is re-enabled when item in list is selected
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e) {
            Close();
        }

        private void LimitnvpstateSizeChanged(object sender, EventArgs e) {
            if (WindowState == FormWindowState.Minimized && Screen.GetWorkingArea(this).Contains(Cursor.Position)) {
                ShowInTaskbar = false;
                notifyIcon.Visible = true;
                Hide();
            }
        }

        private void NotifyIconMouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                WindowState = FormWindowState.Normal;
                ShowInTaskbar = true;
                notifyIcon.Visible = false;
                Show();
            } else if (e.Button == MouseButtons.Right) {
                notifyIcon.ContextMenuStrip = contextMenuStrip1;
            }
        }

        private void StartMinimizedToolStripMenuItemClick(object sender, EventArgs e) {
            using (var config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {
                config.SetValue("StartMinimized", Convert.ToString(startMinimizedToolStripMenuItem.Checked), RegistryValueKind.String);
            }
        }

        private void LimitnvpstateFormClosing(object sender, FormClosingEventArgs e) {
            LimitPstate(false);
        }

        private void ProcessesSelectedIndexChanged(object sender, EventArgs e) {
            // only enable the remove process button if a index is selected
            removeProcess.Enabled = processes.SelectedIndex > -1;
        }

        private void DonateToolStripMenuItem_Click(object sender, EventArgs e) {
            _ = Process.Start("https://www.buymeacoffee.com/amitxv");
        }
    }
}
