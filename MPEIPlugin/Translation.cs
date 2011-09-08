using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
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

      Log.Info("[MPEI] Using language " + lang);

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
          Log.Warn("[MPEI] Cannot find translation file {0}.  Failing back to English", langPath);
        else
        {
          Log.Error("[MPEI] Error in translation xml file: {0}. Failing back to English", lang);
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
            Log.Error("[MPEI] Error in Translation Engine");
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
          Log.Info("[MPEI] Translation not found for field: {0}.  Using hard-coded English default.", fi.Name);
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
    public static string All = "All";
    public static string AlwaysCheckForUpdates = "Always check for updates";
    public static string Actions = "Actions";
    public static string ActionAdded = "Action was added to queue";
    public static string ActionRemoved = "Action was removed from queue";
    public static string Author = "Author";
    public static string AskForRestart = "This Operation will restart MediaPortal.\nDo you wish to continue?";    

    // C
    public static string ChangeLog = "Change Log";

    // D
    public static string Download = "Download";
    public static string Disable = "Disable";

    // E
    public static string Exit = "Exit";
    public static string Enable = "Enable";

    // F

    // G

    // H

    // I
    public static string Id = "Id";
    public static string Install = "Install";
    public static string InstalledExtensions = "Installed Extensions";
    
    // L
    
    // M
    public static string MPOnlineExtensions = "MP Online Extensions";

    // N
    public static string Name = "Extensions";
    public static string NeverCheckForUpdates = "Never check for updates";
    public static string NewExtensions = "New Extensions";
    public static string NewUpdates = "New updates";
    public static string NewVersion = "New version";
    public static string Notification = "Notification";
    public static string NotificationWarning = "There are installation tasks still outstanding.\nRestart and execute now?";
    public static string NotificationMessage = "Would you like to restart MediaPortal\nand execute pending tasks now?";

    // O
    public static string OnlineExtensions = "Online Extensions";
   

    // P

    // R
    public static string ReleaseDate = "Release date";
    public static string Restart = "Restart";
    public static string Rotate = "Rotate";
    public static string RevokeLastAction = "Revoke last action";
    public static string RestartNow = "This operation requires a restart of\nMediaPortal. Do you want to restart now?\nIf not, the task will queued.";
    

    // S
    public static string Sections = "Sections";
    public static string Settings = "Settings";
    public static string ShowSreenshots = "Show Screenshots";
    public static string ShowChangelogs = "Show Change Logs";
    public static string SelectVersion = "Select version";
    public static string SelectVersionToInstall = "Select version to (Re)Install";
    public static string StartSlideshow = "Start slideshow";

    // T


    // U
    public static string UseSilent = "Install extension silently?";
    public static string Update = "Update";
    public static string UpdateAvailable = "Update Available";
    public static string UpdateAll = "Update All";
    public static string Updates = "Updates";
    public static string Uninstall = "Uninstall";
    public static string UnKnownFileType = "Installation is not possible\nUnknown file type detected!";

    // V
    public static string Views = "Views";
    public static string Version = "Version";

    // W

    // Y

    #endregion

  }

}