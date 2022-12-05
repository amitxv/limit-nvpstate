using Microsoft.Win32;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace limit_nvpstate {
    public partial class limitnvpstate : Form {

        private ArrayList processListen = new ArrayList();
        private Process inspector = new Process();

        public limitnvpstate() {
            InitializeComponent();
        }

        private void addProcess_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) {
                processes.Items.Add(Path.GetFileName(ofd.FileName));
            }
        }

        private void removeProcess_Click(object sender, EventArgs e) {
            processes.Items.RemoveAt(processes.SelectedIndex);
        }

        private void eventHandler(object sender, EventArrivedEventArgs e) {
            Process createdProcess = Process.GetProcessById((int)(uint)e.NewEvent.Properties["ProcessID"].Value);

            if (processListen.Contains(createdProcess.ProcessName)) {
                unlimitPstate(true);
                createdProcess.WaitForExit();
                unlimitPstate(false);
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

            // repopulate backend array
            processListen.Clear();
            for (int i = 0; i < processes.Items.Count; i++) {
                processListen.Add(processes.Items[i].ToString().Split('.')[0]);
            }
        }

        private void unlimitPstate(bool enabled) {
            if (enabled) {
                inspector.StartInfo.Arguments = $"-setPStateLimit:{gpuIndex.SelectedItem},0";
            } else {
                inspector.StartInfo.Arguments = $"-setPStateLimit:{gpuIndex.SelectedItem},{pstateLimit.SelectedItem.ToString().Replace("P", "")}";
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
            unlimitPstate(false); // limit pstates when program starts

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
            unlimitPstate(true);
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void processes_SelectedIndexChanged(object sender, EventArgs e) {
            if (processes.SelectedIndex > -1) {
                removeProcess.Enabled= true;
            } else {
                removeProcess.Enabled= false;
            }
        }
    }
}
