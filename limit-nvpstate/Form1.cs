using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace limit_nvpstate {
    public partial class limitnvpstate : Form {
        private Process inspector = new Process();
        private string version = "0.2.0";

        public limitnvpstate() {
            InitializeComponent();
        }

        private void addProcess_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) {
                string fileName = Path.GetFileName(ofd.FileName).Replace(".exe", "");
                processes.Items.Add(fileName);
            }
        }

        private void removeProcess_Click(object sender, EventArgs e) {
            processes.Items.RemoveAt(processes.SelectedIndex);
        }

        private void eventHandler(object sender, EventArrivedEventArgs e) {
            Process createdProcess = Process.GetProcessById((int)(uint)e.NewEvent.Properties["ProcessID"].Value);

            if (processes.Items.Contains(createdProcess.ProcessName)) {
                limitPstate(false);
                createdProcess.WaitForExit();
                limitPstate(true);
            }
        }

        private void loadSettings() {
            using (RegistryKey config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {

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
                foreach (string i in (string[])config.GetValue("ProcessList")) {
                    processes.Items.Add(i);
                }
            }
        }

        private void limitPstate(bool enabled) {
            string index = gpuIndex.SelectedItem.ToString().Split('-')[0].Replace(" ", "");

            if (enabled) {
                inspector.StartInfo.Arguments = $"-setPStateLimit:{index},{pstateLimit.SelectedItem.ToString().Replace("P", "")}";
            } else {
                inspector.StartInfo.Arguments = $"-setPStateLimit:{index},0";
            }
            inspector.Start();
        }

        private void applySettings_Click(object sender, EventArgs e) {
            using (RegistryKey config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {
                config.SetValue("LimitPState", pstateLimit.SelectedIndex, RegistryValueKind.String);
                config.SetValue("IndexOfGPU", gpuIndex.SelectedIndex, RegistryValueKind.String);
                config.SetValue("ProcessList", processes.Items.OfType<string>().ToArray(), RegistryValueKind.MultiString);
            }
            loadSettings();
        }

        private void limitnvpstate_Load(object sender, EventArgs e) {
            // create a new instance of the ManagementObjectSearcher class
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            // call the Get method of the ManagementObjectSearcher class to retrieve a collection of GPU objects
            ManagementObjectCollection gpuCollection = searcher.Get();

            // iterate through the collection of GPU objects
            int index = 0;
            foreach (ManagementObject gpu in gpuCollection) {
                gpuIndex.Items.Add($"{index} - {gpu["Name"]}");
                index++;
            }

            if (!File.Exists("nvidiaInspector.exe")) {
                MessageBox.Show("Inspector not found in current directory", "limit-nvpstate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.FormClosing -= limitnvpstate_FormClosing;
                this.Close();
            }

            // configure inspector launch settings
            inspector.StartInfo.FileName = "nvidiaInspector.exe";
            inspector.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // configure event handler
            ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived += new EventArrivedEventHandler(eventHandler);
            startWatch.Start();


            loadSettings(); // load settings when program starts
            limitPstate(true); // limit pstates when program starts

            if (startMinimizedToolStripMenuItem.Checked) {
                this.WindowState = FormWindowState.Minimized;
            }

            removeProcess.Enabled = false; // disable remove button by default, is re-enabled when item in list is selected
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void limitnvpstate_SizeChanged(object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized && Screen.GetWorkingArea(this).Contains(Cursor.Position)) {
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;
                this.Hide();
            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                notifyIcon.Visible = false;
                this.Show();
            } else if (e.Button == MouseButtons.Right) {
                notifyIcon.ContextMenuStrip = contextMenuStrip1;
            }
        }

        private void startMinimizedToolStripMenuItem_Click(object sender, EventArgs e) {
            using (RegistryKey config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {
                config.SetValue("StartMinimized", Convert.ToString(startMinimizedToolStripMenuItem.Checked), RegistryValueKind.String);
            }
        }

        private void limitnvpstate_FormClosing(object sender, FormClosingEventArgs e) {
            limitPstate(false);
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void processes_SelectedIndexChanged(object sender, EventArgs e) {
            // only enable the remove process button if a index is selected
            removeProcess.Enabled = processes.SelectedIndex > -1;
        }
    }
}
