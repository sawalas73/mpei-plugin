
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MpeCore;
using MpeCore.Classes;
using MPEIPlugin.Classes;
using Action = MediaPortal.GUI.Library.Action;

namespace MPEIPlugin
{
  public class GUISettings : GuiBase
  {
    [SkinControlAttribute(50)]
    protected GUIFacadeControl facadeView = null;
    [SkinControlAttribute(2)]
    protected GUIButtonControl btnSections = null;

    public string SettingsFile { get; set; }
    private bool SettingsChanged { get; set; }
    private string GUID { get; set; }
    private ExtensionSettings settings = new ExtensionSettings();

    public delegate void SettingsChangedHandler(string guid);
    public event SettingsChangedHandler OnSettingsChanged;

    public GUISettings()
    {
      GetID = 803;
    }

    public override string GetModuleName()
    {
      return Translation.NameSettings;
    }

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\myextensions2settings.xml");
    }

    protected override void OnPageLoad()
    {
      ClearProperties();

      if (!string.IsNullOrEmpty(_loadParameter) && File.Exists(_loadParameter))
        SettingsFile = _loadParameter;

      if (SettingsFile == null)
      {
        Log.Error("[MPEI] Error Loading SettingsFile, file must exist!");
        GUIWindowManager.ShowPreviousWindow();
        return;
      }

      Match match = Regex.Match(SettingsFile, @"\\Installer\\V\d+\\(?<guid>[^\\]+)\\", RegexOptions.Singleline);
      if (match.Success) GUID = match.Groups["guid"].Value;
        
      settings.Load(SettingsFile);
      PopulateFacade(0);
      base.OnPageLoad();
    }

    void PopulateFacade(int section)
    {
      GUIControl.ClearControl(GetID, facadeView.GetID);
      if (settings.Settings.Count > 1)
        btnSections.Disabled = false;
      else
        btnSections.Disabled = true;
      int i = 0;
      foreach (KeyValuePair<string, List<ExtensionSetting>> settingsection in settings.Settings)
      {
        if (i == section)
        {
          foreach (ExtensionSetting setting in settingsection.Value)
          {
            var item = new GUIListItem {Label = setting.DisplayName, Label2 = setting.DisplayValue, MusicTag = setting};
            item.OnItemSelected += new GUIListItem.ItemSelectedHandler(item_OnItemSelected);
            facadeView.Add(item);
          }
        }
        i++;
      }
      
      GUIControl.SelectItemControl(GetID, facadeView.GetID, 0);
      GUIPropertyManager.SetProperty("#itemcount", facadeView.Count.ToString());
    }

    protected override void OnPageDestroy(int new_windowId)
    {
      MediaPortal.Profile.Settings.SaveCache();

      // signal to plugins that property re-load should occur
      if (OnSettingsChanged != null && SettingsChanged)
        OnSettingsChanged(GUID);

      base.OnPageDestroy(new_windowId);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      base.OnClicked(controlId, control, actionType);
      if (control == btnSections)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;
        dlg.Reset();
        dlg.SetHeading(Translation.Sections);
        foreach (KeyValuePair<string, List<ExtensionSetting>> settingsection in settings.Settings)
        {
          dlg.Add(settingsection.Key);
        }
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1) return;
        PopulateFacade(dlg.SelectedId - 1);
        GUIControl.FocusControl(GetID, facadeView.GetID);
      }

      if (control == facadeView)
      {
        GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECTED, GetID, 0, controlId, 0, 0, null);
        OnMessage(msg);
        int itemIndex = (int)msg.Param1;
        if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
        {
          GUIListItem item = facadeView.SelectedListItem;
          ExtensionSetting set = item.MusicTag as ExtensionSetting;
          GetValue(set);
          item.Label2 = set.DisplayValue;
        }
      }

    }

    private void GetValue(ExtensionSetting setting)
    {
      string settingValue = setting.Value;

      if(setting.Values.Count > 0)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;
        dlg.Reset();
        dlg.SetHeading(setting.DisplayName);
        foreach (string displayValue in setting.DisplayValues)
        {
          dlg.Add(displayValue);
        }
        dlg.selectOption(setting.Value);
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1) return;
        setting.Value = setting.Values[dlg.SelectedId - 1];
      }
      else
      {
        if (GUIUtils.GetStringFromKeyboard(ref settingValue))
        {
          setting.Value = settingValue;
        }
      }

      if (settingValue != setting.Value)
      {
        SettingsChanged = true;
      }

    }

    private void ClearProperties()
    {
      GUIUtils.SetProperty("#MPE.Selection.Description", string.Empty);
    }

    private void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      ExtensionSetting setting = item.MusicTag as ExtensionSetting;
      if (setting == null) return;

      GUIUtils.SetProperty("#MPE.Selection.Description", setting.Description);
    }

  }
}
