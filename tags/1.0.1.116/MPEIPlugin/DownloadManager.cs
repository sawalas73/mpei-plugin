﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using MediaPortal.GUI.Library;

namespace MPEIPlugin
{
  public class DownloadManager
  {
    private WebClient client = new WebClient();
    private Queue<DownLoadInfo> queue = new Queue<DownLoadInfo>();

    public event DownloadDoneEventHadler DownloadDone;
    public event DownloadDoneEventHadler DownloadStart;

    public delegate void DownloadDoneEventHadler(DownLoadInfo info);

    private DownLoadInfo _currentItem = new DownLoadInfo();

    public DownloadManager()
    {
      client.DownloadFileCompleted += client_DownloadFileCompleted;
      client.UseDefaultCredentials = true;
      client.Proxy.Credentials = CredentialCache.DefaultCredentials;
    }

    void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
      if (e.Error == null)
      {
        try
        {
          if (File.Exists(_currentItem.TempFile))
          {
            string dir = Path.GetDirectoryName(_currentItem.Destinatiom);
            if (!Directory.Exists(dir))
              Directory.CreateDirectory(dir);
            File.Copy(_currentItem.TempFile, _currentItem.Destinatiom, true);
            File.Delete(_currentItem.TempFile);
          }          
          if (DownloadDone != null)
            DownloadDone(_currentItem);
        }
        catch (Exception exception)
        {
          Log.Error("[MPEI] Failed to process {0}: {1}", _currentItem.Url, exception.Message);
        }
      }
      else
      {
        Log.Warn("[MPEI] Failed to download update file from {0}: {1}", _currentItem.Url, e.Error.Message);
      }
      if (queue.Count > 0)
        StartDownload();
      else
        DownloadDone(new DownLoadInfo(){ ItemType = DownLoadItemType.UpdateInfoComplete });
    }
    
    public void Download(DownLoadInfo info)
    {
      queue.Enqueue(info);
      if (!client.IsBusy)
        StartDownload();
    }

    private void StartDownload()
    {
      try
      {
        if (client.IsBusy)
          return;
        _currentItem = queue.Dequeue();
        _currentItem.TempFile = Path.GetTempFileName();
        if (DownloadStart != null)
          DownloadStart(_currentItem);
        client.DownloadFileAsync(new Uri(_currentItem.Url), _currentItem.TempFile);
      }
      catch { }
    }

    public void Download(string source, string dest , DownLoadItemType type)
    {
      Download(new DownLoadInfo()
                 {
                   Destinatiom = dest,
                   ItemType = type,
                   TempFile = Path.GetTempFileName(),
                   Url = source
                 });
    }

  }

}