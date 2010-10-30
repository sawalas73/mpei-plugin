using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MPEIHelper
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      string url = "";
      string file = "";
      string name = "";


      string configFile = args[0];
      if (File.Exists(configFile))
      {
        TextReader reader = new StreamReader(configFile);
        url = reader.ReadLine();
        file = reader.ReadLine();
        name = reader.ReadLine();
        KillProcces("Configuration");
        KillProcces("MediaPortal");

        DownloadFile dlg = new DownloadFile();
        dlg.Text = name;
        dlg.StartDownload(url, file);
        System.Diagnostics.Process.Start(file);
        return;
      }

      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1(args[0]));
    }

    private static void KillProcces(string name)
    {
      Process[] prs = Process.GetProcesses();
      foreach (Process pr in prs)
      {
        if (pr.ProcessName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          pr.CloseMainWindow();
          pr.Close();
          Thread.Sleep(500);
        }
      }
      prs = Process.GetProcesses();
      foreach (Process pr in prs)
      {
        if (pr.ProcessName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          try
          {
            Thread.Sleep(5000);
            pr.Kill();
          }
          catch (Exception) { }
        }
      }
    }
  }
}
