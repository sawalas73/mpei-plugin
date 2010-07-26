﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using MediaPortal.Configuration;

namespace MPEIPlugin.Classes
{
  public enum SettingType
  {
    SString,
    SBool,
    SList,
    SInt
  }

  public class ExtensionSetting
  {

    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string EntryName { get; set; }
    public string DefaultValue { get; set; }
    
    private string _value;
    public string Value
    {
      get
      {
        using (var xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
        {
          _value = xmlreader.GetValueAsString(GetName(EntryName), GetName(Name), DefaultValue);
        }
        return _value;
      }
      set
      {
        _value = value;
        using (var xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
        {
          xmlwriter.SetValue(GetName(EntryName), GetName(Name), _value);
        }
      }
    }

    public SettingType Type { get; set; }
    public string ListValues { get; set; }
    public string DisplayListValues { get; set; }

    public List<string> Values
    {
      get
      {
        var vallist = new List<string>();
        string[] val = ListValues.Split('|');
        foreach (string s in val)
        {
          if (!string.IsNullOrEmpty(s))
            vallist.Add(s);
        }
        return vallist;
      }
    }

    public List<string> DisplayValues
    {
      get
      {
        var vallist = new List<string>();
        string[] val = DisplayListValues.Split('|');
        foreach (string s in val)
        {
          if (!string.IsNullOrEmpty(s))
            vallist.Add(s);
        }
        return vallist;
      }
    }

    public string DisplayValue
    {
      get
      {
        string s = Value;
        if(DisplayValues.Count>0)
        {
          int i = 0;
          foreach (string value in Values)
          {
            if (value == s)
              return DisplayValues[i];
            i++;
          }
        }
        return s;
      }
    }

    public ExtensionSetting Load(XmlNode node)
    {
      if (node.Attributes["name"] != null)
        Name = node.Attributes["name"].Value;
      if (node.Attributes["displayname"] != null)
        DisplayName = node.Attributes["displayname"].Value;
      if (node.Attributes["entryname"] != null)
        EntryName = node.Attributes["entryname"].Value;
      if (node.Attributes["defaultvalue"] != null)
        DefaultValue = node.Attributes["defaultvalue"].Value;
      if (node.Attributes["listvalues"] != null)
        ListValues = node.Attributes["listvalues"].Value;
      if (node.Attributes["displaylistvalues"] != null)
        DisplayListValues = node.Attributes["displaylistvalues"].Value;
      if (node.Attributes["type"] != null)
      {
        if (node.Attributes["type"].Value == "string")
          Type = SettingType.SString;
        if (node.Attributes["type"].Value == "bool")
          Type = SettingType.SBool;
        if (node.Attributes["type"].Value == "list")
          Type = SettingType.SList;
        if (node.Attributes["type"].Value == "int")
          Type = SettingType.SInt;

      }
      return this;
    }

    string GetName(string name)
    {
      name = name.Trim();
      if(name.StartsWith("$")&& name.Contains("."))
      {
        string entryName = name.Substring(1).Split('.')[0];
        string valname = name.Substring(1).Split('.')[1];
        using (var xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
        {
          _value = xmlreader.GetValueAsString(entryName, valname, "");
        }
      }
      return name;
    }

  }
}