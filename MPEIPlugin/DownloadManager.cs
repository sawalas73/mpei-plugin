using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using MediaPortal.Configuration;
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

    private List<string> failedDownloads = new List<string>();

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
        Log.Warn("[MPEI] Failed to download file from {0}: {1}", _currentItem.Url, e.Error.Message);
        
        // don't download again in the same session..could add an expire but dont think its nessarcary
        if (_currentItem.ItemType == DownLoadItemType.UpdateInfo || _currentItem.ItemType == DownLoadItemType.Logo)
        {
          if (!failedDownloads.Contains(_currentItem.Url))
            failedDownloads.Add(_currentItem.Url);
        }
      }

      // signal reload of the facade
      if (queue.Count(d => d.ItemType == DownLoadItemType.UpdateInfo) == 0 && _currentItem.ItemType == DownLoadItemType.UpdateInfo)
      {
        DownloadDone(new DownLoadInfo() { ItemType = DownLoadItemType.UpdateInfoComplete });
      }

      // continue on with queue
      if (queue.Count > 0)
        StartDownload();
    }
    
    public void Download(DownLoadInfo info)
    {
      if (failedDownloads.Contains(info.Url)) return;

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
        _currentItem.TempFile = DownloadManager.GetTempFilename();
        if (DownloadStart != null)
          DownloadStart(_currentItem);
        client.DownloadFileAsync(new Uri(_currentItem.Url), _currentItem.TempFile);
      }
      catch { }
    }

    public void Download(string source, string dest , DownLoadItemType type)
    {
      if (failedDownloads.Contains(source)) return;

      Download(new DownLoadInfo()
                 {
                   Destinatiom = dest,
                   ItemType = type,
                   TempFile = DownloadManager.GetTempFilename(),
                   Url = source
                 });
    }

    public static string GetTempFilename()
    {
      string tempFile = string.Empty;
      try
      {
        tempFile = Path.GetTempFileName();
      }
      catch (IOException)
      {
        // Most likely too many files in %temp%
        tempFile = Config.GetFile(Config.Dir.Cache, string.Format(@"mpei-{0}", Guid.NewGuid()));
      }
      return tempFile;
    }

  }

}
