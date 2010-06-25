using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

namespace MPEIPlugin
{
  public static class Translation
  {
    #region Private variables

    //private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Dictionary<string, string> _translations;
    private static readonly string _path = string.Empty;
    private static readonly DateTimeFormatInfo _info;

    #endregion

    #region Constructor

    static Translation()
    {
      string lang;

      try
      {
        lang = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
        _info = DateTimeFormatInfo.GetInstance(CultureInfo.CurrentUICulture);
      }
      catch (Exception)
      {
        lang = CultureInfo.CurrentUICulture.Name;
        _info = DateTimeFormatInfo.GetInstance(CultureInfo.CurrentUICulture);
      }

      Log.Info("Using language " + lang);

      _path = Config.GetSubFolder(Config.Dir.Language, "MPEI");

      if (!System.IO.Directory.Exists(_path))
        System.IO.Directory.CreateDirectory(_path);

      LoadTranslations(lang);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the translated strings collection in the active language
    /// </summary>
    public static Dictionary<string, string> Strings
    {
      get
      {
        if (_translations == null)
        {
          _translations = new Dictionary<string, string>();
          Type transType = typeof(Translation);
          FieldInfo[] fields = transType.GetFields(BindingFlags.Public | BindingFlags.Static);
          foreach (FieldInfo field in fields)
          {
            _translations.Add(field.Name, field.GetValue(transType).ToString());
          }
        }
        return _translations;
      }
    }

    #endregion

    #region Public Methods

    public static int LoadTranslations(string lang)
    {
      XmlDocument doc = new XmlDocument();
      Dictionary<string, string> TranslatedStrings = new Dictionary<string, string>();
      string langPath = "";
      try
      {
        langPath = Path.Combine(_path, lang + ".xml");
        doc.Load(langPath);
      }
      catch (Exception e)
      {
        if (lang == "en")
          return 0; // otherwise we are in an endless loop!

        if (e.GetType() == typeof(FileNotFoundException))
          Log.Warn("Cannot find translation file {0}.  Failing back to English", langPath);
        else
        {
          Log.Error("Error in translation xml file: {0}. Failing back to English", lang);
          Log.Error(e);
        }

        return LoadTranslations("en");
      }
      foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
      {
        if (stringEntry.NodeType == XmlNodeType.Element)
          try
          {
            TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("Field").Value, stringEntry.InnerText);
          }
          catch (Exception ex)
          {
            Log.Error("Error in Translation Engine");
            Log.Error(ex);
          }
      }

      Type TransType = typeof(Translation);
      FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static);
      foreach (FieldInfo fi in fieldInfos)
      {
        if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
          TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
        else
          Log.Info("Translation not found for field: {0}.  Using hard-coded English default.", fi.Name);
      }
      return TranslatedStrings.Count;
    }

    public static string GetByName(string name)
    {
      if (!Strings.ContainsKey(name))
        return name;

      return Strings[name];
    }

    public static string GetByName(string name, params object[] args)
    {
      return String.Format(GetByName(name), args);
    }

    /// <summary>
    /// Takes an input string and replaces all ${named} variables with the proper translation if available
    /// </summary>
    /// <param name="input">a string containing ${named} variables that represent the translation keys</param>
    /// <returns>translated input string</returns>
    public static string ParseString(string input)
    {
      Regex replacements = new Regex(@"\$\{([^\}]+)\}");
      MatchCollection matches = replacements.Matches(input);
      foreach (Match match in matches)
      {
        input = input.Replace(match.Value, GetByName(match.Groups[1].Value));
      }
      return input;
    }


    internal static void SetProperty(string property, string value)
    {
      if (property == null)
        return;

      //// If the value is empty always add a space
      //// otherwise the property will keep 
      //// displaying it's previous value
      if (String.IsNullOrEmpty(value))
        value = " ";

      GUIPropertyManager.SetProperty(property, value);
    }

    public static string GetDayName(DayOfWeek dayOfWeek)
    {
      return _info.GetDayName(dayOfWeek);
    }
    public static string GetShortestDayName(DayOfWeek dayOfWeek)
    {
      return _info.GetShortestDayName(dayOfWeek);
    }

    #endregion

    #region Translations / Strings

    /// <summary>
    /// These will be loaded with the language files content
    /// if the selected lang file is not found, it will first try to load en(us).xml as a backup
    /// if that also fails it will use the hardcoded strings as a last resort.
    /// </summary>


    // A
    public static string AlwaysCheckForUpdates = "Always check for updates";
    public static string Actions = "Actions";
    public static string ActionAdded = "Action was added to action queue";
    public static string ActionRemoved = "Action was removed from action queue";

    // C
    public static string ChangeLogFor = "Change log for :";

    // D
    public static string Download = "Download";

    // E
    public static string Exit = "Exit";

    // F

    // G

    // H

    // I
    public static string Install = "Install";
    public static string InstalledExtensions = "Installed Extensions";

    // L
    
    // M

    // N
    public static string NeverCheckForUpdates = "Never check for updates";
    public static string NewExtensions = "New Extensions";
    public static string NewUpdates = "New updates";
    public static string Notification = "Notification";
    public static string NotificationMsg1 = "There are pending actions";
    public static string NotificationMsg2 = "Do you want to execute it ?";
    public static string NotificationMsg3 = "This operation will restart MediaPortal";
    public static string NotificationMsg4 = "and execute the pending actions";


    // O
    public static string OnlineExtensions = "Online Extensions";

    // P

    // R
    public static string Restart = "Restart";
    public static string Rotate = "Rotate";
    public static string RevokeLastAction = "Revoke last action";

    // S
    public static string ShowSreenshots = "Show sreenshots";
    public static string SelectVersionToInstall = "Select version to (Re)Install";
    public static string StartSlideshow = "Start slideshow";

    // T


    // U
    public static string Update = "Update";
    public static string UpdateAll = "Update All";
    public static string Updates = "Updates";
    public static string Uninstall = "Uninstall";
   
    // V
    public static string Views = "Views";

    // W

    // Y

    #endregion

  }

}