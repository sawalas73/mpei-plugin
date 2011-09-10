
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MediaPortal.Util;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MpeCore;
using MpeCore.Classes;
using MPEIPlugin.MPSite;
using Action = MediaPortal.GUI.Library.Action;


namespace MPEIPlugin
{
  public class GUIMpeiPlugin : GuiBase, ISetupForm, IComparer<GUIListItem>, IShowPlugin
  {
    #region enums
    enum SortMethod
    {
      Name = 0,
      Type = 1,
      Date = 2,
      Download = 3,
      Rating = 4,
    }

    enum View : int
    {
      List = 0,
      Icons = 1,
      LargeIcons = 2,
    }

    enum Views
    {
      Local = 0,
      Online = 1,
      Updates = 2,
      New = 3,
      Queue = 4,
      MpSIte = 5
    }

    #endregion

    #region Base variabeles
    View currentLayout = View.List;
    Views currentListing = Views.Local;
    SortMethod currentSortMethod = SortMethod.Date;
    bool sortAscending = true;
    private MPSiteUtil SiteUtil = new MPSiteUtil();

    string currentFolder = string.Empty;
    int selectedItemIndex = -1;
    private DownloadManager _downloadManager = new DownloadManager();
    static GUIDialogProgress _dlgProgress;

    private ApplicationSettings _setting = new ApplicationSettings();

    private Timer _timer = new Timer(4000);

    private const string UpdateIndexUrl = "http://install.team-mediaportal.com/MPEI/extensions.txt";

    private const int newdays = 10;
    #endregion

    #region SkinControls

    [SkinControlAttribute(50)]
    protected GUIFacadeControl facadeView = null;
    [SkinControlAttribute(2)]
    protected GUIButtonControl btnViewAs = null;
    [SkinControlAttribute(3)]
    protected GUISortButtonControl btnSortBy = null;
    [SkinControlAttribute(5)]
    protected GUIButtonControl btnRestart = null;
    [SkinControlAttribute(6)]
    protected GUIButtonControl btnViews = null;
    //[SkinControlAttribute(7)]
    //protected GUIButtonControl btnUpdate = null;
    [SkinControlAttribute(8)]
    protected GUIButtonControl btnUpdateAll = null;

    #endregion

    public GUIMpeiPlugin()
    {

    }

    public override string GetModuleName()
    {
      return Translation.Name;
    }

    public override bool Init()
    {
      Log.Debug("[MPEI] Init Start");
      MpeInstaller.Init();
      queue = QueueCommandCollection.Load();
      _downloadManager.DownloadDone += _downloadManager_DownloadDone;
      _timer.Elapsed += _timer_Elapsed;
      _setting = ApplicationSettings.Load();
      MpeInstaller.InstalledExtensions.IgnoredUpdates = _setting.IgnoredUpdates;
      MpeInstaller.KnownExtensions.IgnoredUpdates = _setting.IgnoredUpdates;
      MpeInstaller.KnownExtensions.Hide(_setting.ShowOnlyStable, _setting.ShowOnlyCompatible);
      currentFolder = string.Empty;
      
      GenerateProperty();
      foreach (string name in Translation.Strings.Keys)
      {
        GUIUtils.SetProperty("#MPEI.Translation." + name + ".Label", Translation.Strings[name]);
      }

      bool bResult = Load(GUIGraphicsContext.Skin + @"\myextensions2.xml");

      GUIGraphicsContext.Receivers += GUIGraphicsContext_Receivers;
      _timer.Enabled = true;

      SiteUtil.LoadCatTree();
      Log.Debug("[MPEI] Init End");

      return bResult;
    }

    void GUIGraphicsContext_Receivers(GUIMessage message)
    {
      if (message.Message == GUIMessage.MessageType.GUI_MSG_CLICKED)
      {
        GUIButtonControl cnt =
          GUIWindowManager.GetWindow(message.TargetWindowId).GetControl(message.SenderControlId) as GUIButtonControl;
        if (cnt != null)
        {
          string desc = cnt.Description;
          if (!string.IsNullOrEmpty(desc) && desc.Split(':').Length > 1)
          {
            string command = desc.Split(':')[0].Trim().ToUpper();
            string arg = desc.Split(':')[1].Trim().Replace("_", "-");
            switch (command)
            {
              case "MPEIUPDATE":
                UpdateExtension(arg);
                break;
              case "MPEISHOWCHANGELOG":
                ShowChangeLog(arg);
                break;
              case "MPEIINSTALL":
                InstallExtension(arg);
                break;
              case "MPEIUNINSTALL":
                UnInstallExtension(arg);
                break;
              case "MPEICONFIGURE":
                ConfigureExtension(arg);
                break;
              default:
                break;
            }
          }
        }
      }
    }

    void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      _timer.Enabled = false;
      if(queue.Items.Count > 0)
      {
       if(AskForRestartWarning()) 
         RestartMP();
      }
      else
      {
        if (_setting.DoUpdateInStartUp)
          Log.Info("[MPEI] Next download of updates scheduled for {0}", _setting.LastUpdate.AddDays(_setting.UpdateDays));

        int i = DateTime.Now.Subtract(_setting.LastUpdate).Days;
        if (_setting.DoUpdateInStartUp && i >= _setting.UpdateDays)
        {
          Log.Info("[MPEI] Download of updates is required, downloading now...");
          _downloadManager.Download(UpdateIndexUrl, Path.GetTempFileName(), DownLoadItemType.IndexList);
          _setting.LastUpdate = DateTime.Now;
          _setting.Save();
        }
      }
    }

    #region downloadmanager

    void _downloadManager_DownloadDone(DownLoadInfo info)
    {
      switch (info.ItemType)
      {
        case DownLoadItemType.IndexList:
          {
            LoadExtensionIndex(info.Destinatiom);
            DownloadUpdateInfo();
          }
          break;
        case DownLoadItemType.UpdateInfo:
          {
            MpeInstaller.KnownExtensions.Add(ExtensionCollection.Load(info.Destinatiom));
            MpeInstaller.KnownExtensions.Hide(_setting.ShowOnlyStable, _setting.ShowOnlyCompatible);
            
            Log.Debug("[MPEI] Update info loaded from {0}", info.Url);
            File.Delete(info.Destinatiom);
            GenerateProperty();
            MpeInstaller.Save();
            if (_setting.UpdateAll)
            {
              UpdateAll();
            }
          }
          break;
        case DownLoadItemType.UpdateInfoComplete:
          Log.Info("[MPEI] Finished downloading updates for extensions");
          if (GUIWindowManager.ActiveWindow == GetID && currentListing != Views.MpSIte)
            LoadDirectory(currentFolder);
          break;
        case DownLoadItemType.Extension:
          break;
        case DownLoadItemType.Logo:
          if (info.Package != null)
            Logo(info.Package, info.ListItem);
          if (info.SiteItem != null)
            Logo(info.SiteItem, info.ListItem);
          break;
        case DownLoadItemType.Other:
          break;
        default:
          //throw new ArgumentOutOfRangeException();
          break;
      }
    }
    
    
    #endregion

    #region ISetupForm Members

    public bool CanEnable()
    {
      return true;
    }

    public string PluginName()
    {
      return "Extensions";
    }

    public bool HasSetup()
    {
      return false;
    }

    public bool DefaultEnabled()
    {
      return true;
    }

    public int GetWindowId()
    {
      return GetID;
    }

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = Translation.Name;
      strButtonImage = string.Empty;
      strButtonImageFocus = string.Empty;
      strPictureImage = "hover_extensions.png";
      return true;
    }

    public string Author()
    {
      return "Dukus, Migue, ltfearme";
    }

    public string Description()
    {
      return "Browse (Un)Install Extensions";
    }

    public void ShowPlugin()
    {

    }

    #endregion

    #region IShowPlugin Members

    public bool ShowDefaultHome()
    {
      return false;
    }

    #endregion

    void DownloadUpdateInfo()
    {
      List<string> onlineFiles = MpeInstaller.InstalledExtensions.GetUpdateUrls(new List<string>());
      onlineFiles = MpeInstaller.KnownExtensions.GetUpdateUrls(onlineFiles);
      onlineFiles = MpeInstaller.GetInitialUrlIndex(onlineFiles);
      foreach (string onlineFile in onlineFiles)
      {
        _downloadManager.Download(onlineFile, Path.GetTempFileName(), DownLoadItemType.UpdateInfo);
      }
    }

    static void LoadExtensionIndex(string tempUpdateIndex)
    {
      if (File.Exists(tempUpdateIndex))
      {
        var indexUrls = new List<string>();
        string[] lines = File.ReadAllLines(tempUpdateIndex);
        foreach (string line in lines)
        {
          if (string.IsNullOrEmpty(line)) continue;
          if (line.StartsWith("#")) continue;

          indexUrls.Add(line.Split(';')[0]);
        }
        MpeInstaller.SetInitialUrlIndex(indexUrls);
      }
    }

    void UpdateAll()
    {
      var updatelist = new Dictionary<PackageClass, PackageClass>();
      foreach (PackageClass packageClass in MpeInstaller.InstalledExtensions.Items)
      {
        PackageClass update = MpeInstaller.KnownExtensions.GetUpdate(packageClass);
        if (update == null)
          continue;
        updatelist.Add(packageClass, update);
      }
      foreach (KeyValuePair<PackageClass, PackageClass> valuePair in updatelist)
      {
        if (valuePair.Value == null)
          continue;
        DownloadExtension(valuePair.Value);
        queue.Add(new QueueCommand(valuePair.Value, CommandEnum.Install));
      }
      if (queue.Items.Count > 0)
        queue.Save();
        //NotifyUser();
    }

    List<PackageClass> GetUpdates()
    {
      List<PackageClass> updates = new List<PackageClass>();
      foreach (PackageClass packageClass in MpeInstaller.InstalledExtensions.Items)
      {
        PackageClass update = MpeInstaller.KnownExtensions.GetUpdate(packageClass);
        if (update == null)
          continue;
        updates.Add(update);
      }
      return updates;
    }

    #region Serialisation
    void LoadSettings()
    {
      _dlgProgress = (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);

      using (MediaPortal.Profile.Settings xmlreader = new MPSettings())
      {

        string tmpLine = string.Empty;
        tmpLine = xmlreader.GetValue("myextensions2", "viewby");
        if (tmpLine != null)
        {
          if (tmpLine == "list") currentLayout = View.List;
          else if (tmpLine == "icons") currentLayout = View.Icons;
          else if (tmpLine == "largeicons") currentLayout = View.LargeIcons;
        }

        tmpLine = (string)xmlreader.GetValue("myextensions2", "sort");
        if (tmpLine != null)
        {
          if (tmpLine == "name") currentSortMethod = SortMethod.Name;
          else if (tmpLine == "type") currentSortMethod = SortMethod.Type;
          else if (tmpLine == "date") currentSortMethod = SortMethod.Date;
          else if (tmpLine == "download") currentSortMethod = SortMethod.Download;
          else if (tmpLine == "rate") currentSortMethod = SortMethod.Rating;
        }
        tmpLine = (string)xmlreader.GetValue("myextensions2", "listing");
        if (tmpLine != null)
        {
          if (tmpLine == "local") currentListing = Views.Local;
          else if (tmpLine == "online") currentListing = Views.Online;
          else if (tmpLine == "updates") currentListing = Views.Updates;
          else if (tmpLine == "new") currentListing = Views.New;
          else if (tmpLine == "queue") currentListing = Views.Queue;
          else if (tmpLine == "site") currentListing = Views.MpSIte;
        }
        sortAscending = xmlreader.GetValueAsBool("myextensions2", "sortascending", true);
      }
    }

    void SaveSettings()
    {
      using (MediaPortal.Profile.Settings xmlwriter = new MPSettings())
      {
        switch (currentLayout)
        {
          case View.List:
            xmlwriter.SetValue("myextensions2", "viewby", "list");
            break;
          case View.Icons:
            xmlwriter.SetValue("myextensions2", "viewby", "icons");
            break;
          case View.LargeIcons:
            xmlwriter.SetValue("myextensions2", "viewby", "largeicons");
            break;
        }

        switch (currentSortMethod)
        {
          case SortMethod.Name:
            xmlwriter.SetValue("myextensions2", "sort", "name");
            break;
          case SortMethod.Type:
            xmlwriter.SetValue("myextensions2", "sort", "type");
            break;
          case SortMethod.Date:
            xmlwriter.SetValue("myextensions2", "sort", "date");
            break;
          case SortMethod.Download:
            xmlwriter.SetValue("myextensions2", "sort", "download");
            break;
          case SortMethod.Rating:
            xmlwriter.SetValue("myextensions2", "sort", "rate");
            break;
        }

        switch (currentListing)
        {
          case Views.Local:
            xmlwriter.SetValue("myextensions2", "listing", "local");
            break;
          case Views.Online:
            xmlwriter.SetValue("myextensions2", "listing", "online");
            break;
          case Views.Updates:
            xmlwriter.SetValue("myextensions2", "listing", "updates");
            break;
          case Views.New:
            xmlwriter.SetValue("myextensions2", "listing", "new");
            break;
          case Views.Queue:
            xmlwriter.SetValue("myextensions2", "listing", "queue");
            break;
          case Views.MpSIte:
            xmlwriter.SetValue("myextensions2", "listing", "site");
            break;
        }

        xmlwriter.SetValueAsBool("myextensions2", "sortascending", sortAscending);
      }
    }
    #endregion

    #region BaseWindow Members

    public override int GetID
    {
      get
      {
        return 801;
      }
    }

    public override void OnAction(Action action)
    {
      if (action.wID == Action.ActionType.ACTION_PREVIOUS_MENU)
      {
        if (facadeView.Focus)
        {
          GUIListItem item = facadeView[0];
          if (item != null)
          {
            if (item.IsFolder && item.Label == "..")
            {
              LoadDirectory(item.Path);
              return;
            }
          }
        }
      }

      if (action.wID == Action.ActionType.ACTION_PARENT_DIR)
      {
        GUIListItem item = facadeView[0];
        if (item != null)
        {
          if (item.IsFolder && item.Label == "..")
          {
            LoadDirectory(item.Path);
          }
        }
        return;
      }

      base.OnAction(action);
    }

    protected override void OnPageLoad()
    {
      LoadSettings();
      switch (currentSortMethod)
      {
        case SortMethod.Name:
          btnSortBy.SelectedItem = 0;
          break;
        case SortMethod.Type:
          btnSortBy.SelectedItem = 1;
          break;
        case SortMethod.Date:
          btnSortBy.SelectedItem = 2;
          break;
        case SortMethod.Download:
          btnSortBy.SelectedItem = 3;
          break;
        case SortMethod.Rating:
          btnSortBy.SelectedItem = 4;
          break;
      }


      SelectCurrentItem();
      UpdateButtonStates();
      LoadDirectory(currentFolder);

      btnSortBy.SortChanged += SortChanged;
      _askForRestart = true;

      base.OnPageLoad();
    }

    protected override void OnShowContextMenu()
    {
      GUIListItem item = facadeView.SelectedListItem;
      SiteItems si = item.MusicTag as SiteItems;
      if (si != null)
      {
        GUIDialogMenu dlg1 = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
        if (dlg1 == null) return;
        dlg1.Reset();
        dlg1.Add(Translation.ShowSreenshots);
        dlg1.DoModal(GetID);
        if (dlg1.SelectedId == -1) return;
        si.LoadInfo();
        if (dlg1.SelectedLabelText == Translation.ShowSreenshots)
        {
          GUISlideShow SlideShow = (GUISlideShow)GUIWindowManager.GetWindow(802);
          if (SlideShow == null)
          {
            return;
          }

          SlideShow.Reset();
          foreach (string files in si.Images)
          {
            if (!string.IsNullOrEmpty(files))
              SlideShow.Add(files);
          }

          if (SlideShow.Count > 0)
          {
            //Thread.Sleep(1000);
            GUIWindowManager.ActivateWindow(802);
            SlideShow.StartSlideShow();
          }

        }

        return;
      }

      PackageClass pk = item.MusicTag as PackageClass;
      if (pk == null)
        return;
      PackageClass installedpak = MpeInstaller.InstalledExtensions.Get(pk.GeneralInfo.Id);

      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null) return;
      dlg.Reset();
      if (queue.Get(pk.GeneralInfo.Id) != null)
      {
        dlg.SetHeading(string.Format("{0} {1} v{2}", Translation.Revoke, queue.Get(pk.GeneralInfo.Id).CommandEnum == CommandEnum.Install ? Translation.Install : Translation.Uninstall, queue.Get(pk.GeneralInfo.Id).TargetVersion.ToString()));
        dlg.Add(Translation.RevokeLastAction);
      }
      else
      {
        dlg.SetHeading(Translation.Actions);
        dlg.Add(Translation.Install);
        if (installedpak != null && MpeInstaller.KnownExtensions.GetUpdate(installedpak) != null)
        {
          dlg.Add(Translation.Update);
        }
        if (MpeInstaller.InstalledExtensions.Get(pk) != null)
        {
          dlg.Add(Translation.Uninstall);
        }
        dlg.Add(_setting.IgnoredUpdates.Contains(pk.GeneralInfo.Id)
                  ? Translation.AlwaysCheckForUpdates
                  : Translation.NeverCheckForUpdates);
      }
      if (!string.IsNullOrEmpty(pk.GeneralInfo.Params[ParamNamesConst.ONLINE_SCREENSHOT].Value.Trim()) && pk.GeneralInfo.Params[ParamNamesConst.ONLINE_SCREENSHOT].Value.Split(ParamNamesConst.SEPARATORS).Length > 0)
      {
        dlg.Add(Translation.ShowSreenshots);
      }
      dlg.Add(Translation.ShowChangelogs);

      GetPackageConfigFile(pk);
      if (MpeInstaller.InstalledExtensions.Get(pk) != null && File.Exists(pk.LocationFolder + "extension_settings.xml"))
      {
        dlg.Add(Translation.Settings);
      }

      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1) return;
      if (dlg.SelectedLabelText == Translation.Install)
      {
        ShowInstall(pk);
      }
      else if (dlg.SelectedLabelText == Translation.Uninstall)
      {
        queue.Add(new QueueCommand(pk, CommandEnum.Uninstall));
        NotifyUser();
      }
      else if (dlg.SelectedLabelText == Translation.RevokeLastAction)
      {
        queue.Remove(pk.GeneralInfo.Id);
        NotifyRemoveUser();
      }
      else if (dlg.SelectedLabelText == Translation.Update)
      {
        queue.Add(new QueueCommand(MpeInstaller.KnownExtensions.GetUpdate(installedpak), CommandEnum.Install));
        NotifyUser();
      }
      else if (dlg.SelectedLabelText == Translation.NeverCheckForUpdates)
      {
        _setting.IgnoredUpdates.Add(pk.GeneralInfo.Id);
      }
      else if (dlg.SelectedLabelText == Translation.AlwaysCheckForUpdates)
      {
        _setting.IgnoredUpdates.Remove(pk.GeneralInfo.Id);
      }
      else if (dlg.SelectedLabelText == Translation.ShowSreenshots)
      {
        GUISlideShow SlideShow = (GUISlideShow)GUIWindowManager.GetWindow(802);
        if (SlideShow == null)
        {
          return;
        }

        SlideShow.Reset();
        foreach (string files in pk.GeneralInfo.Params[ParamNamesConst.ONLINE_SCREENSHOT].Value.Split(ParamNamesConst.SEPARATORS))
        {
          if (!string.IsNullOrEmpty(files))
            SlideShow.Add(files);
        }

        if (SlideShow.Count > 0)
        {
          //Thread.Sleep(1000);
          GUIWindowManager.ActivateWindow(802);
          SlideShow.StartSlideShow();
        }
      }
      else if (dlg.SelectedLabelText == Translation.ShowChangelogs)
      {
        ShowChangeLog(pk);
      }
      else if (dlg.SelectedLabelText == Translation.Settings)
      {
        GUISettings guiSettings = (GUISettings)GUIWindowManager.GetWindow(803);
        //guiSettings.SettingsFile = pk.LocationFolder + "extension_settings.xml";
        GUIWindowManager.ActivateWindow(803,guiSettings.SettingsFile = pk.LocationFolder + "extension_settings.xml");
      }
      _setting.Save();
      queue.Save();
      
    }
    protected override void OnPageDestroy(int newWindowId)
    {
      selectedItemIndex = facadeView.SelectedListItemIndex;
      SaveSettings();
      if (queue.Items.Count > 0)
      {
        if (GUIUtils.ShowYesNoDialog(Translation.Notification, Translation.NotificationWarning, true))
        {
          RestartMP();
        }
      }
      base.OnPageDestroy(newWindowId);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      base.OnClicked(controlId, control, actionType);

      if (control == btnViewAs)
      {
        bool shouldContinue = false;
        do
        {
          shouldContinue = false;
          switch (currentLayout)
          {
            case View.List:
              currentLayout = View.Icons;
              if (facadeView.ThumbnailLayout == null)
                shouldContinue = true;
              else
                facadeView.CurrentLayout = GUIFacadeControl.Layout.SmallIcons;
              break;

            case View.Icons:
              currentLayout = View.LargeIcons;
              if (facadeView.ThumbnailLayout == null)
                shouldContinue = true;
              else
                facadeView.CurrentLayout = GUIFacadeControl.Layout.LargeIcons;
              break;

            case View.LargeIcons:
              currentLayout = View.List;
              if (facadeView.ListLayout == null)
                shouldContinue = true;
              else
                facadeView.CurrentLayout = GUIFacadeControl.Layout.List;
              break;
          }
        } while (shouldContinue);

        SelectCurrentItem();
        GUIControl.FocusControl(GetID, controlId);
        return;
      } //if (control == btnViewAs)

      if (control == btnSortBy)
      {
        OnShowSort();
      }

      if (control == btnRestart)
      {
        if (queue.Items.Count > 0)
        {
          if (GUIUtils.ShowYesNoDialog(Translation.Notification, Translation.NotificationMessage, true))
          {
            RestartMP();
          }
        }
      }

      if (control == btnViews)
      {
        OnShowViews();
        GUIControl.FocusControl(GetID, facadeView.GetID);
      }

      if (control == btnUpdateAll)
      {
        UpdateAll();
        LoadDirectory(currentFolder);
        GUIControl.FocusControl(GetID, facadeView.GetID);
      }

      if (control == facadeView)
      {
        GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECTED, GetID, 0, controlId, 0, 0, null);
        OnMessage(msg);
        int itemIndex = (int)msg.Param1;
        if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
        {
          OnClick(itemIndex);
        }
      }
    }

    void NotifyUser()
    {
      if (_askForRestart)
      {
        if (GUIUtils.ShowYesNoDialog(Translation.Notification, Translation.RestartNow, true))
        {
          RestartMP();
          return;
        }
      }

      _askForRestart = false;

      GUIUtils.ShowNotifyDialog(Translation.Notification, Translation.ActionAdded, 2);
      LoadDirectory(currentFolder);
    }

    void NotifyRemoveUser()
    {
      GUIUtils.ShowNotifyDialog(Translation.Notification, Translation.ActionRemoved, 2);
      LoadDirectory(currentFolder);
    }

    void OnClick(int itemIndex)
    {
      GUIListItem item = facadeView.SelectedListItem;
      if (item == null) return;
      Category category = item.MusicTag as Category;
      if (category != null)
      {
        LoadDirectory(category.Id);
        return;
      }

      if (item.IsFolder)
      {
        selectedItemIndex = -1;
        LoadDirectory(item.Path);
      }
      else
      {
        SiteItems si = item.MusicTag as SiteItems;
        if (si != null)
        {
          si.LoadInfo();
          item_OnItemSelected(item, facadeView);
          GUIInfo guiinfo = (GUIInfo)GUIWindowManager.GetWindow(804);
          guiinfo.SiteItem = si;
          guiinfo.queue = queue;
          guiinfo._askForRestart = _askForRestart;
          guiinfo.Package = null;
          GUIWindowManager.ActivateWindow(804);
        }
        else
        {
          PackageClass pk = item.MusicTag as PackageClass;
          GetPackageConfigFile(pk);
          item_OnItemSelected(item, facadeView);
          GUIInfo guiinfo = (GUIInfo) GUIWindowManager.GetWindow(804);
          guiinfo.SiteItem = null;
          guiinfo.queue = queue;
          guiinfo._askForRestart = _askForRestart;
          guiinfo.Package = pk;
          guiinfo.SettingsFile = pk.LocationFolder + "extension_settings.xml";
          GUIWindowManager.ActivateWindow(804);
        }
      }
    }

    void DownloadExtension(PackageClass packageClass)
    {
      string pak = packageClass.LocationFolder + packageClass.GeneralInfo.Id + ".mpe2";
      if (!File.Exists(pak) && !string.IsNullOrEmpty(packageClass.GeneralInfo.OnlineLocation))
        _downloadManager.Download(new DownLoadInfo
        {
          Destinatiom = pak,
          ItemType = DownLoadItemType.Extension,
          Package = packageClass,
          Url = packageClass.GeneralInfo.OnlineLocation
        });
     
    }

    void GenerateProperty()
    {
      foreach (PackageClass item in MpeInstaller.InstalledExtensions.Items)
      {
        PackageClass update = MpeInstaller.KnownExtensions.GetUpdate(item);
        if (update != null)
        {
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".haveupdate", "true");
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".updatelog", update.GeneralInfo.VersionDescription);
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".updatedate",update.GeneralInfo.ReleaseDate.ToShortDateString());
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".updateversion", update.GeneralInfo.Version.ToString());
        }
        else
        {
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".haveupdate", "false");
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".updatelog", " ");
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".updatedate", " ");
          GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".updateversion", " ");
        }
        GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".installedversion", item.GeneralInfo.Version.ToString());
        GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".isinstalled", "true");
      }

      foreach (PackageClass item in MpeInstaller.KnownExtensions.GetUniqueList().Items)
      {
        GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".name", item.GeneralInfo.Name);
        GUIPropertyManager.SetProperty("#mpei." + item.GeneralInfo.Id.Replace("-", "_") + ".author", item.GeneralInfo.Author);
      }

      string s = "";
      GUIPropertyManager.SetProperty("#mpei.updates", " ");
      foreach (PackageClass update in GetUpdates())
      {
        s += update.GeneralInfo.Name + " - " + update.GeneralInfo.Version.ToString() + ".::.";
      }
      if(!string.IsNullOrEmpty(s))
      {
        s = Translation.NewUpdates + " : " + s;
        GUIPropertyManager.SetProperty("#mpei.updates", s);
      }

      s = "";
      GUIPropertyManager.SetProperty("#mpei.newextensions", " ");
      foreach (PackageClass pk in MpeInstaller.KnownExtensions.GetUniqueList().Items)
      {
        if (DateTime.Now.Subtract(pk.GeneralInfo.ReleaseDate).Days < newdays)
        {
          s += pk.GeneralInfo.Name + " - " + pk.GeneralInfo.Version.ToString() + ".::.";
        }
      }
      if (!string.IsNullOrEmpty(s))
      {
        s = Translation.NewExtensions + " : " + s;
        GUIPropertyManager.SetProperty("#mpei.newextensions", s);
      }

    }

    void ShowInstall(PackageClass pk)
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null) return;
      dlg.Reset();
      dlg.SetHeading(Translation.SelectVersionToInstall); 
      ExtensionCollection paks = MpeInstaller.KnownExtensions.GetList(pk.GeneralInfo.Id);
      foreach (PackageClass item in paks.Items)
      {
        GUIListItem guiListItem = new GUIListItem(item.GeneralInfo.Version.ToString());
        if (MpeInstaller.InstalledExtensions.Get(item) != null)
          guiListItem.Selected = true;
        dlg.Add(guiListItem);
      }
      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1) return;
    
      if (!Directory.Exists(paks.Items[dlg.SelectedId - 1].LocationFolder))
        Directory.CreateDirectory(paks.Items[dlg.SelectedId - 1].LocationFolder);

      DownloadExtension(paks.Items[dlg.SelectedId - 1]);
      queue.Add(new QueueCommand(paks.Items[dlg.SelectedId - 1], CommandEnum.Install));
      NotifyUser();
    }

    void OnShowSort()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null) return;
      dlg.Reset();
      dlg.SetHeading(495); // Sort options

      dlg.AddLocalizedString(103); // name
      //dlg.AddLocalizedString(668); // Type
      dlg.AddLocalizedString(104); // date
      //dlg.AddLocalizedString(14016); // download
      //dlg.AddLocalizedString(14017); // rate

      dlg.SelectedLabel = (int)currentSortMethod;

      // show dialog and wait for result
      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1) return;

      switch (dlg.SelectedId)
      {
        case 103:
          currentSortMethod = SortMethod.Name;
          break;
        case 668:
          currentSortMethod = SortMethod.Type;
          break;
        case 104:
          currentSortMethod = SortMethod.Date;
          break;
        case 14016:
          currentSortMethod = SortMethod.Download;
          break;
        case 14017:
          currentSortMethod = SortMethod.Rating;
          break;
        default:
          currentSortMethod = SortMethod.Name;
          break;
      }

      OnSort();
      if (btnSortBy != null)
        GUIControl.FocusControl(GetID, btnSortBy.GetID);
    }

    void OnShowViews()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
      if (dlg == null) return;
      dlg.Reset();
      dlg.SetHeading(Translation.Views); // Sort options
      if (MpeInstaller.InstalledExtensions.Items.Count > 0)
      {
        dlg.Add(Translation.InstalledExtensions); // local
      }
      if (MpeInstaller.KnownExtensions.Items.Count > 0)
      {
        dlg.Add(Translation.OnlineExtensions); // online
      }
      if (GetUpdates().Count > 0)
        dlg.Add(Translation.Updates); // updates
      
      if(queue.Items.Count>0)
        dlg.Add(Translation.Actions);

      dlg.Add(Translation.NewExtensions); // new
      dlg.Add(Translation.MPOnlineExtensions); // mp online

      dlg.SelectedLabel = (int)currentListing;

      // show dialog and wait for result
      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1) return;

      if (dlg.SelectedLabelText == Translation.InstalledExtensions)
        currentListing = Views.Local;

      if (dlg.SelectedLabelText == Translation.OnlineExtensions)
        currentListing = Views.Online;

      if (dlg.SelectedLabelText == Translation.Updates)
        currentListing = Views.Updates;

      if (dlg.SelectedLabelText == Translation.NewExtensions)
        currentListing = Views.New;
      
      if (dlg.SelectedLabelText == Translation.Actions)
        currentListing = Views.Queue;

      if (dlg.SelectedLabelText == Translation.MPOnlineExtensions)
        currentListing = Views.MpSIte;

      LoadDirectory(currentFolder);
    }

    #endregion

    #region Sort Members
    void OnSort()
    {
      SetLabels();
      facadeView.Sort(this);
      UpdateButtonStates();
      SelectCurrentItem();
    }

    public int Compare(GUIListItem item1, GUIListItem item2)
    {
      if (currentListing == Views.MpSIte)
        return -1;
      if (item1 == item2) return 0;
      if (item1 == null) return -1;
      if (item2 == null) return -1;
      if (item1.IsFolder && item1.Label == "..") return -1;
      if (item2.IsFolder && item2.Label == "..") return -1;
      if (item1.IsFolder && !item2.IsFolder) return -1;
      else if (!item1.IsFolder && item2.IsFolder) return 1;
      

      SortMethod method = currentSortMethod;
      bool bAscending = sortAscending;
      PackageClass pk1 = item1.MusicTag as PackageClass;
      PackageClass pk2 = item2.MusicTag as PackageClass;
      
      if(pk1==null && pk2==null)
      {
        if (item1.Label == Translation.All)
          return -1;
        if (item2.Label == Translation.All)
          return -1;
        return item1.Label.CompareTo(item2.Label);
      }

      switch (method)
      {
        case SortMethod.Name:
          if (bAscending)
          {
            return String.Compare(item1.Label, item2.Label, true);
          }
          else
          {
            return String.Compare(item2.Label, item1.Label, true);
          }

        case SortMethod.Type:
          //if (bAscending)
          //{
          //  return String.Compare(pk1.InstallerInfo.Group, pk2.InstallerInfo.Group, true);
          //}
          //else
          //{
          //  return String.Compare(pk2.InstallerInfo.Group, pk1.InstallerInfo.Group, true);
          //}
          return 0;
        case SortMethod.Date:
          if (bAscending)
          {
            return DateTime.Compare(pk1.GeneralInfo.ReleaseDate, pk2.GeneralInfo.ReleaseDate);
          }
          else
          {
            return DateTime.Compare(pk2.GeneralInfo.ReleaseDate, pk1.GeneralInfo.ReleaseDate);
          }
        case SortMethod.Download:
          //if (bAscending)
          //{
          //  return pk1.DownloadCount - pk2.DownloadCount;
          //}
          //else
          //{
          //  return pk2.DownloadCount - pk1.DownloadCount;
          //}
          return 0;
        case SortMethod.Rating:
          //if (bAscending)
          //{
          //  return (int)pk1.VoteValue - (int)pk2.VoteValue;
          //}
          //else
          //{
          //  return (int)pk2.VoteValue - (int)pk1.VoteValue;
          //}
          return 0;
      }
      return 0;
    }
    #endregion

    #region helper func's

    void LoadDirectory(string strNewDirectory)
    {
      GUIWaitCursor.Show();
      selectedItemIndex = facadeView.SelectedListItemIndex;

      GUIControl.ClearControl(GetID, facadeView.GetID);

      //------------
      switch (currentListing)
      {
        case Views.Local:
          {
            GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.InstalledExtensions);
            GUIListItem item = new GUIListItem();
            foreach (PackageClass pk in MpeInstaller.InstalledExtensions.Items)
            {
              item = new GUIListItem();
              item.MusicTag = pk;
              item.IsFolder = false;
              item.Label = pk.GeneralInfo.Name;
              item.Label2 = pk.GeneralInfo.Version.ToString();
              Logo(pk, item);
              item.OnItemSelected += item_OnItemSelected;
              facadeView.Add(item);
            }
          }
          break;
        case Views.Online:
          {
            if (string.IsNullOrEmpty(strNewDirectory))
            {
              GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.OnlineExtensions);
              GUIListItem item = new GUIListItem();
              item.Label = Translation.All;
              item.Path = item.Label;
              item.IsFolder = true;
              item.MusicTag = null;
              item.ThumbnailImage = string.Empty;
              Utils.SetDefaultIcons(item);
              item.OnItemSelected += item_OnItemSelected;
              facadeView.Add(item);

              Dictionary<string, int> TagList = new Dictionary<string, int>();

              foreach (PackageClass pak in MpeInstaller.KnownExtensions.GetUniqueList().Items)
              {
                foreach (var tag in pak.GeneralInfo.TagList.Tags)
                {
                  if (!TagList.ContainsKey(tag))
                    TagList.Add(tag, 1);
                  else
                    TagList[tag]++;
                }
              }
              foreach (KeyValuePair<string, int> tagList in TagList)
              {
                if (tagList.Value > 1)
                {
                  item = new GUIListItem();
                  item.Label = tagList.Key;
                  item.Path = tagList.Key;
                  item.IsFolder = true;
                  item.MusicTag = null;
                  item.ThumbnailImage = string.Empty;
                  Utils.SetDefaultIcons(item);
                  facadeView.Add(item);
                }
              }
            }
            else
            {
              GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.OnlineExtensions + ": " + strNewDirectory);
              GUIListItem item = new GUIListItem();
              item.Label = "..";
              item.Path = string.Empty;
              item.IsFolder = true;
              item.MusicTag = null;
              item.ThumbnailImage = string.Empty;
              Utils.SetDefaultIcons(item);
              facadeView.Add(item);
              foreach (PackageClass pk in MpeInstaller.KnownExtensions.GetUniqueList().Items)
              {
                if ((pk.GeneralInfo.TagList.Tags.Contains(strNewDirectory) || strNewDirectory == Translation.All))
                {
                  item = new GUIListItem();
                  item.MusicTag = pk;
                  item.IsFolder = false;
                  item.Label = pk.GeneralInfo.Name;
                  item.Label2 = pk.GeneralInfo.Version.ToString();
                  Logo(pk, item);
                  item.OnItemSelected += item_OnItemSelected;
                  facadeView.Add(item);
                }
              }
            }
          }
          break;
        case Views.Updates:
          {
            GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.Updates);
            GUIListItem item = new GUIListItem();
            foreach (PackageClass pk in GetUpdates())
            {
              item = new GUIListItem();
              item.MusicTag = pk;
              item.IsFolder = false;
              item.Label = pk.GeneralInfo.Name;
              item.Label2 = pk.GeneralInfo.Version.ToString();
              Logo(pk, item);
              item.OnItemSelected += item_OnItemSelected;
              facadeView.Add(item);
            }
          }
          break;
        case Views.New:
          {
            GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.NewExtensions);
            GUIListItem item = new GUIListItem();
            foreach (PackageClass pk in MpeInstaller.KnownExtensions.GetUniqueList().Items)
            {
              if (DateTime.Now.Subtract(pk.GeneralInfo.ReleaseDate).Days < newdays)
              {
                item = new GUIListItem();
                item.MusicTag = pk;
                item.IsFolder = false;
                item.Label = pk.GeneralInfo.Name;
                item.Label2 = pk.GeneralInfo.Version.ToString();
                Logo(pk, item);
                item.OnItemSelected += item_OnItemSelected;
                facadeView.Add(item);
              }
            }
          }
          break;
        case Views.Queue:
          {
            GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.Actions);
            GUIListItem item = new GUIListItem();
            foreach (QueueCommand command in queue.Items)
            {
              PackageClass pk = MpeInstaller.KnownExtensions.Get(command.TargetId, command.TargetVersion.ToString());
              if (pk != null)
              {
                item = new GUIListItem();
                item.MusicTag = pk;
                item.IsFolder = false;
                item.Label = pk.GeneralInfo.Name+" - "+command.CommandEnum.ToString();
                item.Label2 = pk.GeneralInfo.Version.ToString();
                Logo(pk, item);
                item.OnItemSelected += item_OnItemSelected;
                facadeView.Add(item);
              }
            }
          }
          break;
        case Views.MpSIte:
          {
            GUIPropertyManager.SetProperty("#MPE.View.Name", Translation.MPOnlineExtensions);
            GUIListItem item = new GUIListItem();
            List<Category> cats = new List<Category>();
            Category pareCategory = SiteUtil.GetCat(strNewDirectory);
            if (string.IsNullOrEmpty(strNewDirectory) || strNewDirectory == "0")
            {
              cats = SiteUtil.GetCats("0");
            }
            else
            {
              Category cate = new Category() { Name = "..", Id = pareCategory.PId };
              item = new GUIListItem();
              item.MusicTag = cate;
              item.IsFolder = true;
              item.Label = cate.Name;
              item.Label2 = "";
              //Logo(pk, item);
              item.OnItemSelected += item_OnItemSelected;
              Utils.SetDefaultIcons(item);
              facadeView.Add(item);

              cats = SiteUtil.GetCats(strNewDirectory);
            }
            foreach (Category category in cats)
            {
              item = new GUIListItem();
              item.MusicTag = category;
              item.IsFolder = true;
              item.Label = category.Name;
              item.Label2 = category.Number;
              //Logo(pk, item);
              item.OnItemSelected += item_OnItemSelected;
              Utils.SetDefaultIcons(item);
              facadeView.Add(item);
            }
            if(pareCategory!=null)
            {
              if (pareCategory.SiteItems.Count == 0)
                SiteUtil.LoadItems(pareCategory);
              foreach (SiteItems siteItem in pareCategory.SiteItems)
              {
                item = new GUIListItem();
                item.MusicTag = siteItem;
                item.IsFolder = false;
                item.Label = siteItem.Name;
                //item.Label2 = category.Number;
                Logo(siteItem, item);
                item.OnItemSelected += item_OnItemSelected;
               facadeView.Add(item);
              }
            }

          }
          break;
      }

      //------------
      //set object count label
      GUIPropertyManager.SetProperty("#itemcount", facadeView.Count.ToString());
      SetLabels();
      SwitchLayout();
      OnSort();
      SelectCurrentItem();

      //set selected item
      if (selectedItemIndex >= 0 && currentFolder == strNewDirectory)
        GUIControl.SelectItemControl(GetID, facadeView.GetID, selectedItemIndex);

      // set focus on view button if no items.
      if (facadeView.Count == 0)
        GUIControl.FocusControl(GetID, btnViews.GetID);

      currentFolder = strNewDirectory;
      GUIWaitCursor.Hide();
    }

    void Logo(SiteItems items, GUIListItem listItem)
    {
      string tempFile = Path.Combine(Path.GetTempPath(),
                                     Utils.EncryptLine(items.LogoUrl));
      if (File.Exists(tempFile))
      {
        listItem.IconImage = tempFile;
        listItem.IconImageBig = tempFile;
      }
      else
      {
        _downloadManager.Download(new DownLoadInfo()
                                    {
                                      Destinatiom = tempFile,
                                      ItemType = DownLoadItemType.Logo,
                                      ListItem = listItem,
                                      //Package = packageClass,
                                      SiteItem = items,
                                      Url = items.LogoUrl
                                    });
      }
    }

    string Logo(PackageClass packageClass, GUIListItem listItem)
    {
      string logofile = "";
      if (Directory.Exists(packageClass.LocationFolder))
      {
        if (File.Exists(packageClass.LocationFolder + "icon.png"))
          logofile = packageClass.LocationFolder + "icon.png";
        if (File.Exists(packageClass.LocationFolder + "icon.jpg"))
          logofile = packageClass.LocationFolder + "icon.jpg";
      }

      if (MpeInstaller.KnownExtensions.GetUpdate(packageClass) != null)
      {
        string signfile = "";
        if (File.Exists(GUIGraphicsContext.Skin + @"\media\extension_update.png"))
          signfile = GUIGraphicsContext.Skin + @"\media\extension_update.png";

        if (string.IsNullOrEmpty(logofile))
        {
          listItem.IconImage = signfile;
          listItem.IconImageBig = signfile;
          return signfile;
        }

        string tempFile = Path.Combine(Path.GetTempPath(),
                                       Utils.EncryptLine("Update" + packageClass.GeneralInfo.Id +
                                                         packageClass.GeneralInfo.Version));
        if (!File.Exists(tempFile))
        {
          Graphics myGraphic = null;
          Image imgB = Image.FromFile(logofile);
          Image imgF = Image.FromFile(signfile);

          myGraphic = System.Drawing.Graphics.FromImage(imgB);
          //myGraphic.DrawImage(imgB, 0, 0, imgB.Width, imgB.Height);
          myGraphic.DrawImage(imgF, 0, (imgB.Height / 2) - 1, imgB.Width / 2, imgB.Height / 2);
          myGraphic.Save();
          imgB.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
        }

        listItem.IconImage = tempFile;
        listItem.IconImageBig = tempFile;
        return tempFile;
      }

      if (queue.Get(packageClass.GeneralInfo.Id) != null)
      {
        string signfile = "";
        if (File.Exists(GUIGraphicsContext.Skin + @"\media\extension_action.png"))
          signfile = GUIGraphicsContext.Skin + @"\media\extension_action.png";

        if (string.IsNullOrEmpty(logofile))
        {
          listItem.IconImage = signfile;
          listItem.IconImageBig = signfile;
          return signfile;
        }

        string tempFile = Path.Combine(Path.GetTempPath(),
                           Utils.EncryptLine("Action" + packageClass.GeneralInfo.Id +
                                             packageClass.GeneralInfo.Version));
        if (!File.Exists(tempFile))
        {
          Graphics myGraphic = null;
          Image imgB = Image.FromFile(logofile);
          Image imgF = Image.FromFile(signfile);
          myGraphic = System.Drawing.Graphics.FromImage(imgB);
          //myGraphic.DrawImage(imgB, 0, 0, imgB.Width, imgB.Height);
          myGraphic.DrawImage(imgF, 0, (imgB.Height / 2) - 1, imgB.Width / 2, imgB.Height / 2);
          myGraphic.Save();
          myGraphic.Save();
          imgB.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
        }
        listItem.IconImage = tempFile;
        listItem.IconImageBig = tempFile;

        return tempFile;

      }
      if (File.Exists(logofile))
      {
        listItem.IconImage = logofile;
        listItem.IconImageBig = logofile;
      }
      else
      {
        logofile = packageClass.LocationFolder + "icon.png";
        string url = packageClass.GeneralInfo.Params[ParamNamesConst.ONLINE_ICON].Value;
        if (!string.IsNullOrEmpty(url))
        {
          _downloadManager.Download(new DownLoadInfo()
                                      {
                                        Destinatiom = logofile,
                                        ItemType = DownLoadItemType.Logo,
                                        ListItem = listItem,
                                        Package = packageClass,
                                        Url = url
                                      });
        }
      }
      return logofile;
    }

    void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      SiteItems siteItem = item.MusicTag as SiteItems;
      if (siteItem != null)
      {
        GUIPropertyManager.SetProperty("#MPE.Selected.Id", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.Name", siteItem.Name);
        GUIPropertyManager.SetProperty("#MPE.Selected.Version", siteItem.Version);
        GUIPropertyManager.SetProperty("#MPE.Selected.Author", siteItem.Author);
        GUIPropertyManager.SetProperty("#MPE.Selected.Description", siteItem.Descriptions);
        GUIPropertyManager.SetProperty("#MPE.Selected.VersionDescription", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.ReleaseDate", siteItem.DateUpdated);
        GUIPropertyManager.SetProperty("#MPE.Selected.Icon", item.IconImageBig);
        GUIPropertyManager.SetProperty("#selectedthumb", item.IconImageBig);
        GUIPropertyManager.SetProperty("#MPE.Selected.JustAded", siteItem.JustAdded ? "true" : "false");
        GUIPropertyManager.SetProperty("#MPE.Selected.Popular", siteItem.Popular ? "true" : "false");
        GUIPropertyManager.SetProperty("#MPE.Selected.DeveloperPick", siteItem.EditorPick ? "true" : "false");
        return;
      }

      PackageClass pak = item.MusicTag as PackageClass;
      if (pak != null)
      {
        PackageClass update = MpeInstaller.KnownExtensions.GetUpdate(pak);
        if (update != null)
        {
          GUIPropertyManager.SetProperty("#MPE.Selected.haveupdate", "true");
          GUIPropertyManager.SetProperty("#MPE.Selected.updatelog", update.GeneralInfo.VersionDescription);
          GUIPropertyManager.SetProperty("#MPE.Selected.updatedate", update.GeneralInfo.ReleaseDate.ToShortDateString());
          GUIPropertyManager.SetProperty("#MPE.Selected.updateversion", update.GeneralInfo.Version.ToString());
        }
        else
        {
          GUIPropertyManager.SetProperty("#MPE.Selected.haveupdate", "false");
          GUIPropertyManager.SetProperty("#MPE.Selected.updatelog", " ");
          GUIPropertyManager.SetProperty("#MPE.Selected.updatedate", " ");
          GUIPropertyManager.SetProperty("#MPE.Selected.updateversion", " ");
        }

        PackageClass installed = MpeInstaller.InstalledExtensions.Get(pak);
        if (installed != null)
        {
          GUIPropertyManager.SetProperty("#MPE.Selected.installedversion",installed.GeneralInfo.Version.ToString());
          GUIPropertyManager.SetProperty("#MPE.Selected.isinstalled", "true");
        }
        else
        {
          GUIPropertyManager.SetProperty("#MPE.Selected.installedversion", " ");
          GUIPropertyManager.SetProperty("#MPE.Selected.isinstalled", "false");
        }

        GUIPropertyManager.SetProperty("#MPE.Selected.Id", pak.GeneralInfo.Id);
        GUIPropertyManager.SetProperty("#MPE.Selected.Name", pak.GeneralInfo.Name);
        GUIPropertyManager.SetProperty("#MPE.Selected.Version", pak.GeneralInfo.Version.ToString());
        GUIPropertyManager.SetProperty("#MPE.Selected.Author", pak.GeneralInfo.Author);
        GUIPropertyManager.SetProperty("#MPE.Selected.Description", pak.GeneralInfo.ExtensionDescription);
        GUIPropertyManager.SetProperty("#MPE.Selected.VersionDescription", pak.GeneralInfo.VersionDescription);
        GUIPropertyManager.SetProperty("#MPE.Selected.ReleaseDate", pak.GeneralInfo.ReleaseDate.ToShortDateString());
        GUIPropertyManager.SetProperty("#MPE.Selected.Icon", item.IconImageBig);
        GUIPropertyManager.SetProperty("#selectedthumb", item.IconImageBig);
      }
      else
      {
        GUIPropertyManager.SetProperty("#MPE.Selected.installedversion", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.isinstalled", "false");

        GUIPropertyManager.SetProperty("#MPE.Selected.ReleaseDate", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.haveupdate", "false");
        GUIPropertyManager.SetProperty("#MPE.Selected.updatelog", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.updatedate", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.updateversion", " ");

        GUIPropertyManager.SetProperty("#MPE.Selected.JustAded", "false ");
        GUIPropertyManager.SetProperty("#MPE.Selected.Popular", "false");
        GUIPropertyManager.SetProperty("#MPE.Selected.DeveloperPick", "false");

        GUIPropertyManager.SetProperty("#MPE.Selected.Id", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.Name", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.Version", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.Author", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.Description", " ");
        GUIPropertyManager.SetProperty("#MPE.Selected.VersionDescription", " ");
        GUIPropertyManager.SetProperty("#selectedthumb", " ");
      }
    }

    void SelectCurrentItem()
    {
      int iItem = facadeView.SelectedListItemIndex;
      if (iItem > -1)
      {
        GUIControl.SelectItemControl(GetID, facadeView.GetID, iItem);
      }
      UpdateButtonStates();
    }

    void UpdateButtonStates()
    {
      facadeView.IsVisible = false;
      facadeView.IsVisible = true;
      //GUIControl.FocusControl(GetID, facadeView.GetID);

      string strLine = string.Empty;
      View view = currentLayout;
      switch (view)
      {
        case View.List:
          strLine = GUILocalizeStrings.Get(101);
          break;
        case View.Icons:
          strLine = GUILocalizeStrings.Get(100);
          break;
        case View.LargeIcons:
          strLine = GUILocalizeStrings.Get(417);
          break;
      }
      if (btnViewAs != null)
      {
        btnViewAs.Label = strLine;
      }

      switch (currentSortMethod)
      {
        case SortMethod.Name:
          strLine = GUILocalizeStrings.Get(103);
          break;
        case SortMethod.Type:
          strLine = GUILocalizeStrings.Get(668);
          break;
        case SortMethod.Date:
          strLine = GUILocalizeStrings.Get(104);
          break;
        case SortMethod.Download:
          strLine = GUILocalizeStrings.Get(14016);
          break;
        case SortMethod.Rating:
          strLine = GUILocalizeStrings.Get(14017);
          break;
      }
      if (btnSortBy != null)
      {
        btnSortBy.Label = strLine;
        btnSortBy.IsAscending = sortAscending;
      }

      btnRestart.Disabled = !(queue.Items.Count > 0);
    }

    void SwitchLayout()
    {
      switch (currentLayout)
      {
        case View.List:
          facadeView.CurrentLayout = GUIFacadeControl.Layout.List;
          break;
        case View.Icons:
          facadeView.CurrentLayout = GUIFacadeControl.Layout.SmallIcons;
          break;
        case View.LargeIcons:
          facadeView.CurrentLayout = GUIFacadeControl.Layout.LargeIcons;
          break;
      }
      UpdateButtonStates();
    }

    void SortChanged(object sender, SortEventArgs e)
    {
      sortAscending = e.Order != System.Windows.Forms.SortOrder.Descending;

      OnSort();
      UpdateButtonStates();

      GUIControl.FocusControl(GetID, ((GUIControl)sender).GetID);
    }

    void SetLabels()
    {
      SortMethod method = currentSortMethod;
      for (int i = 0; i < facadeView.Count; ++i)
      {
        GUIListItem item = facadeView[i];
        if (item.MusicTag != null)
        {
          PackageClass pak = item.MusicTag as PackageClass;
          if (pak != null)
          {
            switch (method)
            {
              case SortMethod.Name:
                item.Label2 = pak.GeneralInfo.Version.ToString();
                break;
              case SortMethod.Type:
                //item.Label2 = pak.InstallerInfo.Group;
                break;
              case SortMethod.Date:
                item.Label2 = pak.GeneralInfo.ReleaseDate.ToShortDateString();
                break;
              case SortMethod.Download:
                //              item.Label2 = pak.DownloadCount.ToString();
                break;
              case SortMethod.Rating:
                //              item.Label2 =((int) pak.VoteValue).ToString();
                break;
              default:
                break;
            }
          }
          if (method == SortMethod.Name)
          {
          }
        }
      }
    }
    #endregion

  }
}
