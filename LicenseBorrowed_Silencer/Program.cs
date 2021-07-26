using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ConsoleApp1_ReadLockedFile
{
    internal class Program
    {
        private const string ApplicationName = "LicenseBorrowed_Silencer";
        private const string AutoStartupLocation = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            var listProcesses = Process.GetProcessesByName(ApplicationName).ToList();
            if (listProcesses.Count > 1)
            {
                Trace.TraceError("Another instance of this application is already running, we will close them first.");
                var current = Process.GetCurrentProcess().Id;
                listProcesses.Where(item => item.Id != current).ToList().ForEach(item => GracefullyCloseProcess(item));
                System.Threading.Thread.Sleep(500);
                listProcesses = Process.GetProcessesByName(ApplicationName).ToList();
                if (listProcesses.Count > 1)
                    listProcesses.Where(item => item.Id != current).ToList().ForEach(proc => KillProcess(proc));
                else Trace.Write($"Gracefully closed!{Environment.NewLine}");
            }

            var handle = GetConsoleWindow();
            // Hide if no arguments
            if (args.Any())
                ShowWindow(handle, SW_HIDE);

            try
            {
                // AddApplicationToStartup();
                RemoveApplicationFromStartup();
                Trace.WriteLine("Now monitoring for bad windows to close...");
                Trace.Indent();
                // Loop for infinity!
                while (true)
                {
                    var list = Process.GetProcessesByName("LMU").Where(proc => proc.MainWindowTitle.Equals("License Borrowed", StringComparison.CurrentCulture)).ToList();
                    while (list.Count > 0)
                    {
                        Trace.Indent();
                        list.ForEach(proc => GracefullyCloseProcess(proc));
                        System.Threading.Thread.Sleep(2000);
                        list = Process.GetProcessesByName("LMU").Where(proc => proc.MainWindowTitle.Equals("License Borrowed", StringComparison.CurrentCulture)).ToList();
                        if (list.Count > 0)
                            list.ForEach(proc => KillProcess(proc));
                        else Trace.Write($"Gracefully closed!{Environment.NewLine}");
                        Trace.Unindent();
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        public static void AddApplicationToStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutoStartupLocation, true))
            {
                key.SetValue(ApplicationName, "\"" + Assembly.GetEntryAssembly().Location + "\"");
            }
        }

        public static void RemoveApplicationFromStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutoStartupLocation, true))
            {
                key.DeleteValue(ApplicationName, false);
            }
        }

        private static void KillProcess(Process proc)
        {
            proc.Kill();
            Trace.WriteLine($"Forcefully killed process: {proc.ProcessName}[{proc.Id}]");
        }

        private static void GracefullyCloseProcess(Process proc)
        {
            proc.CloseMainWindow();
            Trace.Write($"Trying to gracefully close the process: {proc.ProcessName}[{proc.Id}] ...");
        }
    }
}