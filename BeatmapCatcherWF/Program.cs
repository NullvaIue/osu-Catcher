﻿using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace BeatmapCatcher
{
	static class Program
	{
		public static MainForm mainForm;
		public static SettingsForm settingsForm;
		static string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		static string VersionNum = "1.0";
		public static Settings settings;
		public static string[] ExeArgs;
		public static FileSystemWatcher Watcher;
		public static List<String> imagePaths = new List<String>();

		[STAThread]
		static void Main(string[] args)
		{
			ExeArgs = args;

			if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
			{
				MessageBox.Show("Beatmap Catcher already running. Only one instance of this application is allowed");
				return;
			}

			try
			{
				if (System.IO.File.Exists(exeDirectory + "\\settings.json"))
				{
					settings = JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText(exeDirectory + "\\settings.json"));
					mainForm = new MainForm();
					settingsForm = new SettingsForm();
				}
				else
				{
					settings = new Settings();
					if (System.IO.File.Exists(exeDirectory + "\\settings.json"))
						System.IO.File.Create(exeDirectory + "\\settings.json");

					settings.writeSettings();

					mainForm = new MainForm();
					settingsForm = new SettingsForm();
					mainForm.log("WARNING: Settings not found, generating default settings.json file.");
				}

				if (ExeArgs.Length != 0 && ExeArgs[0] == "-s" && settings.StartMinimized)
					mainForm.Minimized = true;

				mainForm.log("Beatmap Catcher Version " + VersionNum);

				mainForm.Minimized = settings.StartMinimized;
				settingsForm.setRunCheck(settings.RunOnStartup);
				settingsForm.setMinCheck(settings.StartMinimized);
				settingsForm.setPathBox(settings.OsuPath);
				settingsForm.setCloseCheck(settings.MinimizeOnClose);

				Watcher = new FileSystemWatcher
				{
					NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
					Filter = "*.*",
					IncludeSubdirectories = true
				};

				Watcher.Deleted += new FileSystemEventHandler(OnDeleted);

				if (Directory.Exists(settings.OsuPath + "\\Songs\\"))
				{
					if (settings.StartMinimized)
						mainForm.Minimized = true;

					Watcher.Path = settings.OsuPath + "\\Songs\\";

					startWatch();
				} else {
					mainForm.log("ERROR: Valid Osu installation not found in: " + settings.OsuPath);
				}

				Application.EnableVisualStyles();
				Application.Run(mainForm);
			} catch (Exception e) {
				mainForm.log("ERROR: Initializing application\n" + e.Message + "\n" + e.StackTrace);
			}
		}

		public static void CreateStartupLnk()
		{
			try
			{
				WshShellClass wshShell = new WshShellClass();
				IWshRuntimeLibrary.IWshShortcut shortcut;

				shortcut = (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + Application.ProductName + ".lnk");

				shortcut.TargetPath = Application.ExecutablePath;
				shortcut.WorkingDirectory = Application.StartupPath;
				shortcut.Arguments = "-s";
				shortcut.Save();
			} catch (Exception e) {
				mainForm.log("ERROR: Creating shortcut\n" + e.Message + "\n" + e.StackTrace);
			}
		}

		public static void DeleteStartupLnk()
		{
			try
			{
				System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + Application.ProductName + ".lnk");
			} catch (Exception e) {
				mainForm.log("ERROR: Deleting  shortcut\n" + e.Message + "\n" + e.StackTrace);
			}
		}

		public static void startWatch()
		{
			try
			{
				Watcher.EnableRaisingEvents = true;
				mainForm.Started = true;
				mainForm.setStateButton("Stop");

				mainForm.log("Started watching for beatmaps in " + Watcher.Path);
			} catch (Exception e) {
				mainForm.log("ERROR: Failed while starting the fileWatcher\n" + e.Message + "\n" + e.StackTrace);
			}
		}

		public static void stopWatch()
		{
			try
			{
				Watcher.EnableRaisingEvents = false;
				mainForm.Started = false;
				mainForm.setStateButton("Start");

				mainForm.log("Stopped watching for beatmaps in " + Watcher.Path);
			}
			catch (Exception e)
			{
				mainForm.log("ERROR: Failed while stopping the fileWatcher\n" + e.Message + "\n" + e.StackTrace);
			}
		}

		public static string getExtension(String path) { return path.Split('.')[path.Split('.').Length - 1]; }

		public static void parseOsu(string path)
		{
			using (StreamReader read = new StreamReader(path))
			{
				while (true)
				{
					string line = read.ReadLine();

					if (line == null)
						break;

					if (line == "[Events]")
					{
						line = read.ReadLine();
						line = read.ReadLine();

						if (line.IndexOf("Video,") != -1)
							line = read.ReadLine();

						int Index = line.IndexOf("0,0,\"");
						int lastIndex = line.LastIndexOf("\"");

						if (Index < 0)
							break;

						Index = line.IndexOf("\"");

						string image = path.Substring(0, path.LastIndexOf('\\')) + "\\" + line.Substring(Index + 1, lastIndex - Index - 1);

						imagePaths.Add(image);

						break;
					}
				}
			}
		}

		private static void OnDeleted(object source, FileSystemEventArgs e)
		{
			try
			{
				if (getExtension(e.FullPath).Contains("osz"))
				{
					string dir = e.FullPath.Substring(0, e.FullPath.Length - 4).Replace(".", "");
					string[] osuFiles = Directory.GetFiles(dir, "*.osu");

					foreach (string s in osuFiles)
						parseOsu(s);

					for (int i = imagePaths.Count - 1; i >= 0; i--)
					{
						if (System.IO.File.Exists(imagePaths[i]))
						{
							mainForm.log("Deleted background [" + imagePaths[i].Substring(imagePaths[i].LastIndexOf('\\') + 1) + "] from beatmap [" + imagePaths[i].Substring(0, imagePaths[i].LastIndexOf('\\')).Substring(imagePaths[i].Substring(0, imagePaths[i].LastIndexOf('\\')).LastIndexOf('\\') + 1) + "]");
							System.IO.File.Delete(imagePaths[i]);
						}

						imagePaths.RemoveAt(i);
					}
				}
			} catch (Exception ex)
			{
				mainForm.log("ERROR: Deleting  background\n" + ex.Message + "\n" + ex.StackTrace);
			}
		}
	}
}