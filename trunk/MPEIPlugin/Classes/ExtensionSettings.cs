using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;

namespace MPEIPlugin.Classes
{
  public class ExtensionSettings
  {
    public Dictionary<string, List<ExtensionSetting>> Settings { get; set; }

    public ExtensionSetting DisableSetting { get; set; }

    public ExtensionSettings()
    {
      Settings = new Dictionary<string, List<ExtensionSetting>>();
      DisableSetting = new ExtensionSetting();
    }

    public void Load(string file)
    {
      Settings.Clear();
      DisableSetting = new ExtensionSetting();

      XmlDocument doc = new XmlDocument();
      if(File.Exists(file))
      {
        doc.Load(file);
        XmlNode xml_file = doc.DocumentElement.SelectSingleNode("/extension_settings");
        DisableSetting.Load(doc.DocumentElement.SelectSingleNode("/extension_settings/disable_entry/setting"));
        XmlNodeList sections = xml_file.SelectNodes("settings");
        if (sections != null)
          foreach (XmlNode section in sections)
          {
            if (section.Attributes != null)
            {
              List<ExtensionSetting> set;
              string sectionname = section.Attributes["section"].Value;
              if (Settings.ContainsKey(sectionname))
                set = Settings[sectionname];
              else
              {
                set = new List<ExtensionSetting>();
                Settings.Add(sectionname, set);
              }
              XmlNodeList xml_settings = section.SelectNodes("setting");
              foreach (XmlNode node in xml_settings)
              {
                set.Add(new ExtensionSetting().Load(node));
              }
            }
          }
      }
    }
  }
}
