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
    private static SplashScreen splashScreen = new SplashScreen();

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      string configFile = args[0];
      if (File.Exists(configFile))
      {
        TextReader reader = new StreamReader(configFile);
        string url = reader.ReadLine();
        string file = reader.ReadLine();
        string name = reader.ReadLine();
        string silen = reader.ReadLine();
        string bkfile = reader.ReadLine();
        reader.Close();
        KillProcces("Configuration");
        KillProcces("MediaPortal");
        DownloadFile dlg = new DownloadFile();
        if (File.Exists(bkfile) && !String.IsNullOrEmpty(silen))
        {
          splashScreen.SetImg(bkfile);
          splashScreen.Show();
        }
        dlg.Client.DownloadProgressChanged += Client_DownloadProgressChanged;
        dlg.Client.DownloadFileCompleted += Client_DownloadFileCompleted;
        dlg.Text = name;
        dlg.StartDownload(url, file);
        string tempfile = file;
        if (File.Exists(file))
        {
          if (Path.GetExtension(file).ToLower() == ".mpe1")
          {
            silen = file + " " + silen;
            file = "mpeinstaller.exe";
          }
          try
          {
            if (File.Exists(file))
            {
              if (splashScreen.Visible)
              {
                splashScreen.ResetProgress();
                splashScreen.SetInfo("Installing extension.Please wait");
              }
              Process process = string.IsNullOrEmpty(silen) ? Process.Start(file) : Process.Start(file, silen);
              process.WaitForExit();
            }
          }
          catch (Exception exception)
          {
            if (splashScreen.Visible)
            {
              splashScreen.Close();
            }
            MessageBox.Show(exception.Message);
          }
          try
          {
            File.Delete(configFile);
            File.Delete(tempfile);
          }
          catch (Exception)
          {

          }
        }
        Process.Start("MediaPortal.exe");
        Thread.Sleep(3000);
        if (splashScreen.Visible)
        {
          splashScreen.Close();
        }
        return;
      }

      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1(args[0]));
    }

    static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
      if (splashScreen.Visible)
      {
        splashScreen.ResetProgress();
      }
    }

    static void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
    {
      if(splashScreen.Visible)
      {
        splashScreen.SetProgress("Downloading", e.ProgressPercentage);
      }
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
