using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MpeCore;
using MpeCore.Classes;
using MPEIPlugin.MPSite;

namespace MPEIPlugin
{
  public class GuiBase : GUIWindow
  {
    public bool _askForRestart = true;
    public QueueCommandCollection queue = new QueueCommandCollection();
    public SiteItems SiteItem = null;


    public void GetPackageConfigFile(PackageClass pk)
    {
      string configfile = pk.LocationFolder + "extension_settings.xml";
      if (!File.Exists(configfile) && File.Exists(pk.LocationFolder + pk.GeneralInfo.Id + ".mpe2"))
      {
        pk = pk.ZipProvider.Load(pk.LocationFolder + pk.GeneralInfo.Id + ".mpe2");
        foreach (FileItem fileItem in pk.UniqueFileList.Items)
        {
          if (Path.GetFileName(fileItem.DestinationFilename).ToLower() == "extension_settings.xml")
            pk.ZipProvider.Extract(fileItem, configfile);
        }

      }
    }

    public void ConfigureExtension(string id)
    {
      PackageClass pak = MpeInstaller.InstalledExtensions.Get(id);

      if (pak != null)
      {
        GetPackageConfigFile(pak);
        GUISettings guiSettings = (GUISettings)GUIWindowManager.GetWindow(803);
        guiSettings.SettingsFile = pak.LocationFolder + "extension_settings.xml";
        GUIWindowManager.ActivateWindow(803);
      }
      else
      {
        Log.Error("The {0} extension isn't installed", id);
      }
    }

    public void UnInstallExtension(string id)
    {
      PackageClass pak = MpeInstaller.InstalledExtensions.Get(id);

      if (pak != null)
      {
        queue.Add(new QueueCommand(pak, CommandEnum.Uninstall));
        queue.Save();
        NotifyUser();
      }
      else
      {
        Log.Error("The {0} extension isn't installed", id);
      }
    }

    public void InstallExtension(string id)
    {
      PackageClass pak = MpeInstaller.KnownExtensions.Get(id);
      if (pak != null)
      {
        queue.Add(new QueueCommand(pak, CommandEnum.Install));
        queue.Save();
        NotifyUser();
      }
      else
      {
        Log.Error("No extension was found :{0}", id);
      }
    }

    public void InstallExtension(SiteItems items)
    {
      items.LoadFileName();

      if (!string.IsNullOrEmpty(items.File) && (Path.GetExtension(items.File) == ".exe" || Path.GetExtension(items.File) == ".mpe1"))
      {
        if(AskForRestart1())
        {
          string conffile = Path.GetTempFileName();
          TextWriter streamWriter = new StreamWriter(conffile, false);
          streamWriter.WriteLine(items.FileUrl);
          streamWriter.WriteLine(Path.Combine(Path.GetTempPath(), items.File));
          streamWriter.WriteLine(items.Name);
          if (Ask(Translation.UseSilent))
            streamWriter.WriteLine("/S");
          else
            streamWriter.WriteLine("");

          using (MediaPortal.Profile.Settings xmlreader = new MPSettings())
          {
            string skinFilePath = ReadSplashScreenXML();
            if (string.IsNullOrEmpty(skinFilePath))
              skinFilePath = ReadReferenceXML();
            bool useFullScreenSplash = xmlreader.GetValueAsBool("general", "usefullscreensplash", true);
            bool startFullScreen = xmlreader.GetValueAsBool("general", "startfullscreen", true);
            if (useFullScreenSplash && startFullScreen)
              streamWriter.WriteLine(skinFilePath);
            else
              streamWriter.WriteLine("");
          }

          streamWriter.Close();
          System.Diagnostics.Process.Start(Config.GetFile(Config.Dir.Base, "MPEIHelper.exe"), conffile);
        }
        return;
      }
      var dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      dlgYesNo.SetHeading(Translation.Notification); //resume movie?
      dlgYesNo.SetLine(1, Translation.InstalledNotPossible);
      dlgYesNo.SetLine(2, Translation.UnKnowFileTYpe);
      dlgYesNo.SetDefaultToYes(true);
      dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
    }

    public void UpdateExtension(string id)
    {
      PackageClass installedpak = MpeInstaller.InstalledExtensions.Get(id);
      if (installedpak != null && MpeInstaller.KnownExtensions.GetUpdate(installedpak) != null)
      {
        queue.Add(new QueueCommand(MpeInstaller.KnownExtensions.GetUpdate(installedpak), CommandEnum.Install));
        queue.Save();
        NotifyUser();
      }
    }

    public void ShowChangeLog(string id)
    {
      PackageClass installedpak = MpeInstaller.InstalledExtensions.Get(id);
      if (installedpak != null)
      {
        PackageClass update = MpeInstaller.KnownExtensions.GetUpdate(installedpak);
        if (update != null)
        {
          GUIDialogText dlgYesNo = (GUIDialogText)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_TEXT);
          if (null == dlgYesNo)
            return;
          dlgYesNo.SetHeading(Translation.ChangeLogFor);
          dlgYesNo.SetText(string.Format("{0} - {1} ", update.GeneralInfo.Name,
                                            update.GeneralInfo.Version.ToString()) + "\n" + update.GeneralInfo.VersionDescription);
          dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
        }
      }
    }


    public void ShowChangeLog(PackageClass pk)
    {
      if (pk == null)
      {
       return;
      }
      string selected = "";
      while (true)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;
        dlg.Reset();
        dlg.SetHeading(Translation.SelectVersion);
        ExtensionCollection paks = MpeInstaller.KnownExtensions.GetList(pk.GeneralInfo.Id);
        foreach (PackageClass item in paks.Items)
        {
          GUIListItem guiListItem = new GUIListItem(item.GeneralInfo.Version.ToString());
          if (MpeInstaller.InstalledExtensions.Get(item) != null)
            guiListItem.Selected = true;
          dlg.Add(guiListItem);
        }
        dlg.selectOption(selected);
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1) return;
        PackageClass ver = MpeInstaller.KnownExtensions.Get(pk.GeneralInfo.Id, dlg.SelectedLabelText);
        selected = dlg.SelectedLabelText;
        if (ver != null)
        {
          GUIDialogText dlgYesNo = (GUIDialogText)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_TEXT);
          if (null == dlgYesNo)
            return;
          dlgYesNo.SetHeading(Translation.ChangeLogFor);
          dlgYesNo.SetText(string.Format("{0} - {1} ", ver.GeneralInfo.Name,
                                         ver.GeneralInfo.Version.ToString()) + "\n" + ver.GeneralInfo.VersionDescription);
          dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
        }
      }
    }

    void NotifyUser()
    {
      if (_askForRestart)
      {
        GUIDialogYesNo dlgYesNo = (GUIDialogYesNo) GUIWindowManager.GetWindow((int) Window.WINDOW_DIALOG_YES_NO);
        if (null == dlgYesNo)
          return;
        dlgYesNo.SetHeading(Translation.Notification);
        dlgYesNo.SetLine(1, "This operation requaied restart of MediaPortal");
        dlgYesNo.SetLine(2, "Do you want to restart now ?");
        dlgYesNo.SetLine(3, "If no the action will be stored in queue");
        dlgYesNo.SetDefaultToYes(true);
        dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
        if (dlgYesNo.IsConfirmed)
        {
          RestartMP();
          return;
        }
      }
      var dlg1 = (GUIDialogNotify) GUIWindowManager.GetWindow((int) Window.WINDOW_DIALOG_NOTIFY);
      if (dlg1 == null) return;
      dlg1.Reset();
      dlg1.SetHeading(Translation.Notification);
      dlg1.SetText(Translation.ActionAdded);
      dlg1.Reset();
      dlg1.TimeOut = 2;
      dlg1.DoModal(GetID);
      _askForRestart = false;

      // LoadDirectory(currentFolder);
    }

    public bool AskForRestart1()
    {
      var dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      dlgYesNo.SetHeading(Translation.Notification); 
      dlgYesNo.SetLine(1, Translation.AskForRestart1);
      dlgYesNo.SetLine(2, Translation.AskForRestart2);
      dlgYesNo.SetDefaultToYes(true);
      dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
      return dlgYesNo.IsConfirmed;
    }

    public bool Ask(string question)
    {
      var dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      dlgYesNo.SetHeading(Translation.Notification);
      dlgYesNo.SetLine(2, question);
      dlgYesNo.SetDefaultToYes(true);
      dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
      return dlgYesNo.IsConfirmed;
    }

    public bool AskForRestart()
    {
      var dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      dlgYesNo.SetHeading(Translation.Notification); //resume movie?
      dlgYesNo.SetLine(1, Translation.NotificationMsg1);
      dlgYesNo.SetLine(2, Translation.NotificationMsg2);
      dlgYesNo.SetLine(3, Translation.NotificationMsg3);
      dlgYesNo.SetDefaultToYes(true);
      dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
      return dlgYesNo.IsConfirmed;
    }

    public void RestartMP()
    {
      string cmdLine = "/MPQUEUE";
      using (MediaPortal.Profile.Settings xmlreader = new MPSettings())
      {
        string m_strSkin = xmlreader.GetValueAsString("skin", "name", "Blue3");
        string skinFilePath = ReadSplashScreenXML();
        if (string.IsNullOrEmpty(skinFilePath))
          skinFilePath = ReadReferenceXML();
        bool useFullScreenSplash = xmlreader.GetValueAsBool("general", "usefullscreensplash", true);
        bool startFullScreen = xmlreader.GetValueAsBool("general", "startfullscreen", true);
        if (useFullScreenSplash && startFullScreen)
          cmdLine += " /BK=\"" + skinFilePath + "\"";
      }
      Log.Debug("MPEI Plugin Start :" + Config.GetFile(Config.Dir.Base, "MPEInstaller.exe ") + cmdLine);
      System.Diagnostics.Process.Start(Config.GetFile(Config.Dir.Base, "MPEInstaller.exe"), cmdLine);
    }


    private string ReadSplashScreenXML()
    {
      string m_strSkin;
      string SkinFilePath = string.Empty;

      // try to find the splashscreen.xml ín the curent skin folder
      using (MediaPortal.Profile.Settings xmlreader = new MPSettings())
      {
        m_strSkin = xmlreader.GetValueAsString("skin", "name", "Blue3");
        SkinFilePath = Config.GetFile(Config.Dir.Skin, m_strSkin + "\\splashscreen.xml");
      }

      XmlDocument doc = new XmlDocument();
      doc.Load(SkinFilePath);
      XmlNodeList ControlsList = doc.DocumentElement.SelectNodes("/window/controls/control");

      foreach (XmlNode Control in ControlsList)
      {
        if (Control.SelectSingleNode("type/text()").Value.ToLower() == "image"
            && Control.SelectSingleNode("id/text()").Value == "1") // if the background image control is found
        {
          string BackgoundImageName = Control.SelectSingleNode("texture/text()").Value;
          string BackgroundImagePath = Config.GetFile(Config.Dir.Skin, m_strSkin + "\\media\\" + BackgoundImageName);
          if (File.Exists(BackgroundImagePath))
          {
            return BackgroundImagePath;
          }
          continue;
        }
      }
      return "";
    }

    private string ReadReferenceXML()
    {
      string m_strSkin;
      string SkinReferenceFilePath = string.Empty;

      using (MediaPortal.Profile.Settings xmlreader = new MPSettings())
      {
        m_strSkin = xmlreader.GetValueAsString("skin", "name", "Blue3");
        SkinReferenceFilePath = Config.GetFile(Config.Dir.Skin, m_strSkin + "\\references.xml");
      }

      XmlDocument doc = new XmlDocument();
      doc.Load(SkinReferenceFilePath);
      XmlNodeList ControlsList = doc.DocumentElement.SelectNodes("/controls/control");

      foreach (XmlNode Control in ControlsList)
      {
        if (Control.SelectSingleNode("type/text()").Value.ToLower() == "image")
        {
          string BackgoundImageName = Control.SelectSingleNode("texture/text()").Value;
          string BackgroundImagePath = Config.GetFile(Config.Dir.Skin, m_strSkin + "\\media\\" + BackgoundImageName);
          if (File.Exists(BackgroundImagePath))
          {
            return BackgroundImagePath; // load the image as background
          }
        }
      }
      return "";
    }
  }
}
