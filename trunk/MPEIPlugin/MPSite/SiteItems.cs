using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using MediaPortal.GUI.Library;

namespace MPEIPlugin.MPSite
{
  public class SiteItems
  {
    public SiteItems()
    {
      Images = new List<string>();
    }

    public string Name { get; set; }
    public string Descriptions { get; set; }
    public string Hits { get; set; }
    public string Url { get; set; }
    public string DateAdded { get; set; }
    public string DateUpdated { get; set; }
    public string Ratig { get; set; }
    public string Votes { get; set; }
    public string Status { get; set; }
    public string LogoUrl { get; set; }
    public string Author { get; set; }
    public string Downloads { get; set; }
    public string FileUrl { get; set; }
    public string Version { get; set; }
    public string File { get; set; }
    public List<string> Images { get; set; }
    public bool EditorPick { get; set; }
    public bool JustAdded { get; set; }
    public bool Popular { get; set; }

    public bool LoadInfo()
    {
      if (string.IsNullOrEmpty(Url))
        return false;

      WebClient client = new WebClient();
      string site = "http://www.team-mediaportal.com" + Url;

      try
      {
        site = client.DownloadString(site);
      }
      catch (WebException e)
      {
        Log.Error("[MPEI] Error Loading info from '{0}': {1}", site, e.Message);
        return false;
      }

      Author = Regex.Match(site, "Submitted By.*?\"><a.*?>(?<name>.*?)</a", RegexOptions.Singleline).Groups["name"].Value;
      Version = Regex.Match(site, "Version.*?\">(?<name>.*?)</div", RegexOptions.Singleline).Groups["name"].Value;
      Downloads = Regex.Match(site, @" Count: </a> \((?<name>.*?) Downloads?", RegexOptions.Singleline).Groups["name"].Value;
      FileUrl = Regex.Match(site, "<div class=\"data-short\"><a href=\"(?<name>.*?)\" t", RegexOptions.Singleline).Groups["name"].Value;
      Descriptions = Regex.Match(site, "<div class=\"listing-desc\"><p>(?<name>.*?)</p>", RegexOptions.Singleline).Groups["name"].Value;
      Descriptions = MediaPortal.Util.Utils.stripHTMLtags(HttpUtility.HtmlDecode(Descriptions));

      Regex regexObj = new Regex("<a rel=\"shadowbox.ca.\".*?href=\"(?<name>.*?)\">", RegexOptions.Singleline);
      Match matchResults = regexObj.Match(site);
      LogoUrl = LogoUrl.Replace("/s/", "/m/");
      Images.Clear();
      while (matchResults.Success)
      {
        Images.Add(matchResults.Groups[1].Value);
        matchResults = matchResults.NextMatch();
      }

      return true;
    }

    public void LoadFileName()
    {
      if(!string.IsNullOrEmpty(File))
        return;
      if (FileUrl.StartsWith("http://www.team-mediaportal.com/"))
      {
        var request = (HttpWebRequest) WebRequest.Create(FileUrl);
        request.Method = "HEAD";
        request.AllowAutoRedirect = true;

        using (var response = request.GetResponse() as HttpWebResponse)
        {
          if (!string.IsNullOrEmpty(response.GetResponseHeader("Content-Disposition")))
            File =
              response.GetResponseHeader("Content-Disposition").Split(';')[1].Split('=')[1].Replace("\"", "").ToLower();
        }
      }
      else
      {
        File = Path.GetFileName(FileUrl);
      }
    }

    public void LoadFields(string fields)
    {
      try
      {
        Regex regexObj = new Regex("class=\"caption\">(?<name>.*?):</span>.*?\"output\">(?<value>.*?)</", RegexOptions.Singleline);
        Match matchResults = regexObj.Match(fields);
        while (matchResults.Success)
        {
          switch (matchResults.Groups["name"].Value)
          {
            case "Hits":
              Hits = matchResults.Groups["value"].Value;
              break;
            case "Votes":
              Votes = matchResults.Groups["value"].Value;
              break;
            case "Date Added":
              DateAdded = matchResults.Groups["value"].Value;
              break;
            case "Last Update":
              DateUpdated = matchResults.Groups["value"].Value;
              break;
            case "Status":
              Status = matchResults.Groups["value"].Value;
              break;
            case "Version":
              Version = matchResults.Groups["value"].Value;
              break;
          }
          matchResults = matchResults.NextMatch();
        }
      }
      catch (ArgumentException)
      {
        // Syntax error in the regular expression
      }
    }
  }
}
