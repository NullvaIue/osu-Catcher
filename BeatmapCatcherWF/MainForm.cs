﻿using System;
using System.IO;
using System.Windows.Forms;

namespace BeatmapCatcher
{
	public partial class MainForm : Form
	{
		public bool Minimized;
		public bool Started = false;

		public MainForm()
		{
			Minimized = Program.settings.StartMinimized;
			InitializeComponent();

			notifyIcon.ContextMenuStrip = contextMenuStrip1;

			if (Minimized)
				showNotifyIcon();

		}

		protected override void SetVisibleCore(bool value)
		{
			if (Minimized)
			{
				value = false;
				if (!this.IsHandleCreated)
					CreateHandle();
			}

			base.SetVisibleCore(value);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (Program.settings.MinimizeOnClose)
			{
				e.Cancel = true;
				showNotifyIcon();
				base.OnFormClosing(e);
			}
		}

		public void setStateButton(string s)
		{
			if (this.stateButton.InvokeRequired)
			{
				this.stateButton.Invoke(new Action<string>(setStateButton), new object[] { s });
				return;
			}

			this.stateButton.Text = s;
		}

		public void log(string str)
		{
			string result;

			if (str != "")
			{
				result = "[" + DateTime.Now.ToString("MM/dd/y hh:mm:ss") + "] " + str + "\n";

				if (logBox.InvokeRequired)
				{
					logBox.Invoke(new Action<string>(log), new object[] { str });
					return;
				}

				logBox.AppendText(result);
			}
		}

		private void showNotifyIcon()
		{
			Hide();

			notifyIcon.Visible = true;
			if (Program.settingsForm != null)
				Program.settingsForm.Visible = false;
		}

		private void logBox_TextChanged(object sender, EventArgs e)
		{
			logBox.SelectionStart = logBox.Text.Length;
			logBox.ScrollToCaret();
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			if (this.WindowState == FormWindowState.Minimized)
			{
				Minimized = true;
				showNotifyIcon();
			}
		}

		private void notifyIcon_DoubleClick(object sender, EventArgs e)
		{
			Minimized = false;
			Show();
			notifyIcon.Visible = false;
			this.WindowState = FormWindowState.Normal;
		}

		private void manualButton_Click(object sender, EventArgs e)
		{
			try
			{
				int count = 0;
				Program.mainForm.log("Manual Scan Started:");

				foreach (string d in Directory.GetDirectories(Program.settings.OsuPath + "\\Songs\\"))
					foreach (string s in Directory.GetFiles(d, "*.osu"))
						Program.parseOsu(s);

				for (int i = Program.imagePaths.Count - 1; i >= 0; i--)
				{
					if (File.Exists(Program.imagePaths[i]))
					{
						File.Delete(Program.imagePaths[i]);
						Program.imagePaths.RemoveAt(i);
						count++;
						Program.mainForm.log("Manually found and deleted background [" + Program.imagePaths[i].Substring(Program.imagePaths[i].LastIndexOf('\\') + 1) + "] from beatmap [" + Program.imagePaths[i].Substring(0, Program.imagePaths[i].LastIndexOf('\\')).Substring(Program.imagePaths[i].Substring(0, Program.imagePaths[i].LastIndexOf('\\')).LastIndexOf('\\') + 1) + "]");
					}
				}

				Program.mainForm.log("Manual Scan Finished (Removed " + count + " backgrounds)");
			} catch (Exception ex) {
				Program.mainForm.log("ERROR: When manually scanning for backgrounds\n" + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void copyButton_Click(object sender, EventArgs e)
		{
			Clipboard.Clear();
			Clipboard.SetText(logBox.Text.Replace("\n", Environment.NewLine));
		}

		private void stateButton_Click(object sender, EventArgs e)
		{
			if (Started)
			{
				Program.stopWatch();
			} else {
				Program.startWatch();
			}
		}

		private void quitMenuItem_Click(object sender, EventArgs e) { System.Environment.Exit(0); }

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e) { Program.settingsForm.Visible = true; }

		private void quitToolStripMenuItem_Click(object sender, EventArgs e) { System.Environment.Exit(0); }

	}
}