﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace limit_nvpstate {
    public partial class limitnvpstate : Form {
        private readonly Process inspector = new Process();

        public limitnvpstate() {
            InitializeComponent();
        }

        private void AddProcess_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) {
                string fileName = Path.GetFileName(ofd.FileName).Replace(".exe", "");
                _ = processes.Items.Add(fileName);
            }
        }

        private void RemoveProcess_Click(object sender, EventArgs e) {
            processes.Items.RemoveAt(processes.SelectedIndex);
        }

        private void EventHandler(object sender, EventArrivedEventArgs e) {
            Process createdProcess = Process.GetProcessById((int)(uint)e.NewEvent.Properties["ProcessID"].Value);

            if (processes.Items.Contains(createdProcess.ProcessName)) {
                LimitPstate(false);
                createdProcess.WaitForExit();
                LimitPstate(true);
            }
        }

        private void LoadSettings() {
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
                    _ = processes.Items.Add(i);
                }
            }
        }

        private void LimitPstate(bool enabled) {
            inspector.StartInfo.Arguments = enabled
                ? $"-setPStateLimit:{gpuIndex.SelectedIndex},{pstateLimit.SelectedItem.ToString().Replace("P", "")}"
                : $"-setPStateLimit:{gpuIndex.SelectedIndex},0";
            _ = inspector.Start();
        }

        private void ApplySettings_Click(object sender, EventArgs e) {
            using (RegistryKey config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {
                config.SetValue("LimitPState", pstateLimit.SelectedIndex, RegistryValueKind.String);
                config.SetValue("IndexOfGPU", gpuIndex.SelectedIndex, RegistryValueKind.String);
                config.SetValue("ProcessList", processes.Items.OfType<string>().ToArray(), RegistryValueKind.MultiString);
            }
            LoadSettings();
        }

        private void Limitnvpstate_Load(object sender, EventArgs e) {
            // create a new instance of the ManagementObjectSearcher class
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            // call the Get method of the ManagementObjectSearcher class to retrieve a collection of GPU objects
            ManagementObjectCollection gpuCollection = searcher.Get();

            // iterate through the collection of GPU objects
            foreach (ManagementObject gpu in gpuCollection.Cast<ManagementObject>()) {
                _ = gpuIndex.Items.Add($"{gpu["Name"]}");
            }

            if (!File.Exists("nvidiaInspector.exe")) {
                _ = MessageBox.Show("Inspector not found in current directory", "limit-nvpstate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                FormClosing -= Limitnvpstate_FormClosing;
                Close();
            }

            // configure inspector launch settings
            inspector.StartInfo.FileName = "nvidiaInspector.exe";
            inspector.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // configure event handler
            ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived += new EventArrivedEventHandler(EventHandler);
            startWatch.Start();


            LoadSettings(); // load settings when program starts
            LimitPstate(true); // limit pstates when program starts

            if (startMinimizedToolStripMenuItem.Checked) {
                WindowState = FormWindowState.Minimized;
            }

            removeProcess.Enabled = false; // disable remove button by default, is re-enabled when item in list is selected
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private void Limitnvpstate_SizeChanged(object sender, EventArgs e) {
            if (WindowState == FormWindowState.Minimized && Screen.GetWorkingArea(this).Contains(Cursor.Position)) {
                ShowInTaskbar = false;
                notifyIcon.Visible = true;
                Hide();
            }
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                WindowState = FormWindowState.Normal;
                ShowInTaskbar = true;
                notifyIcon.Visible = false;
                Show();
            } else if (e.Button == MouseButtons.Right) {
                notifyIcon.ContextMenuStrip = contextMenuStrip1;
            }
        }

        private void StartMinimizedToolStripMenuItem_Click(object sender, EventArgs e) {
            using (RegistryKey config = Registry.CurrentUser.CreateSubKey("SOFTWARE\\limit-nvpstate")) {
                config.SetValue("StartMinimized", Convert.ToString(startMinimizedToolStripMenuItem.Checked), RegistryValueKind.String);
            }
        }

        private void Limitnvpstate_FormClosing(object sender, FormClosingEventArgs e) {
            LimitPstate(false);
        }

        private void ExitToolStripMenuItem1_Click(object sender, EventArgs e) {
            Close();
        }

        private void Processes_SelectedIndexChanged(object sender, EventArgs e) {
            // only enable the remove process button if a index is selected
            removeProcess.Enabled = processes.SelectedIndex > -1;
        }
    }
}
