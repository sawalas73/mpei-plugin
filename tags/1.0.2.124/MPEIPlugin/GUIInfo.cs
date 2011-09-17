
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Text;
using System.Net;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.GUI.Library;
//using MediaPortal.Profile;
using MediaPortal.Util;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MpeCore;
using MpeCore.Classes;
using MPEIPlugin.Classes;
using Action = MediaPortal.GUI.Library.Action;

namespace MPEIPlugin
{
  public class GUIInfo : GuiBase
  {
    [SkinControlAttribute(2)]
    protected GUIButtonControl btnInstall = null;
    [SkinControlAttribute(3)]
    protected GUIButtonControl btnUnInstall = null;
    [SkinControlAttribute(4)]
    protected GUIButtonControl btnUpdate = null;
    [SkinControlAttribute(5)]
    protected GUIButtonControl btnDisable = null;
    [SkinControlAttribute(6)]
    protected GUIButtonControl btnEnable = null;
    [SkinControlAttribute(7)]
    protected GUIButtonControl btnSettings = null;
    [SkinControlAttribute(8)]
    protected GUIButtonControl btnChangeLog = null;

    public string SettingsFile { get; set; }
    private ExtensionSettings settings = new ExtensionSettings();

    public PackageClass Package { get; set; }
    public GUIInfo()
    {
      GetID = 804;
    }

    public override string GetModuleName()
    {
      return Translation.Name;
    }

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\myextensions2info.xml");
    }

    protected override void OnPageLoad()
    {
      GUIPropertyManager.SetProperty("#MPE.Selected.HaveSettings", "false");
      GUIPropertyManager.SetProperty("#MPE.Selected.IsEnabled", "false");
      GUIPropertyManager.SetProperty("#MPE.Selected.IsDisabled", "false");
      if (Package != null)
      {
        settings.Load(SettingsFile);
        PackageClass pak = MpeInstaller.InstalledExtensions.Get(Package);
        checkstate();
        SetFocus();
        if (pak != null)
        {
          if (settings.Settings.Count > 0)
          {
            GUIPropertyManager.SetProperty("#MPE.Selected.HaveSettings", "true");
          }
        }
      }
      base.OnPageLoad();
    }

    void SetFocus()
    {
      if (MpeInstaller.KnownExtensions.GetUpdate(Package) != null)
      {
        if (btnUpdate != null) GUIControl.FocusControl(GetID, btnUpdate.GetID);
      }
      else if (MpeInstaller.InstalledExtensions.Get(Package) != null)
      {
        if (btnUnInstall != null) GUIControl.FocusControl(GetID, btnUnInstall.GetID);
      }
      else
      {
        if (btnInstall != null) GUIControl.FocusControl(GetID, btnInstall.GetID);
      }
    }

    void checkstate()
    {
      if (!string.IsNullOrEmpty(settings.DisableSetting.Name))
      {
        if (settings.DisableSetting.Value == "yes")
        {
          GUIPropertyManager.SetProperty("#MPE.Selected.IsEnabled", "true");
          GUIPropertyManager.SetProperty("#MPE.Selected.IsDisabled", "false");
        }
        else
        {
          GUIPropertyManager.SetProperty("#MPE.Selected.IsEnabled", "false");
          GUIPropertyManager.SetProperty("#MPE.Selected.IsDisabled", "true");
        }
      }
    }

    protected override void OnPageDestroy(int new_windowId)
    {
      MediaPortal.Profile.Settings.SaveCache();
      base.OnPageDestroy(new_windowId);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      base.OnClicked(controlId, control, actionType);
      if (control == btnUpdate)
      {
        UpdateExtension(Package.GeneralInfo.Id);
      }
      if (control == btnUnInstall)
      {
        UnInstallExtension(Package.GeneralInfo.Id);
      }
      if (control == btnSettings)
      {
        ConfigureExtension(Package.GeneralInfo.Id);
      }
      if (control == btnInstall)
      {
        if (Package != null)
          InstallExtension(Package.GeneralInfo.Id);
        if (SiteItem != null)
          InstallExtension(SiteItem);
        
      }

      if (control == btnChangeLog)
      {
        ShowChangeLog(Package);
      }

      if (control == btnEnable)
      {
        settings.DisableSetting.Value = "yes";
        MediaPortal.Profile.Settings.SaveCache();
      }
      if (control == btnDisable)
      {
        settings.DisableSetting.Value = "no";
        MediaPortal.Profile.Settings.SaveCache();
      }
      checkstate();
    }

  }
}
