﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MPEIPlugin.MPSite
{
  public class MPSiteUtil
  {
    
    List<Category> cats = new List<Category>();

    public void LoadCatTree()
    {
      WebClient client = new WebClient();
      string site = client.DownloadString("http://www.team-mediaportal.com/extensions");
      string resultString = null;
      Regex regexObj = new Regex(@"dTree\('(.+?)</script>", RegexOptions.Singleline);
      resultString = regexObj.Match(site).Groups[1].Value;
      try
      {
        Regex regexObj1 = new Regex(@".add.(?<id>.+?),(?<pid>.+?),'(?<name>.+?)<small>.(?<nr>.+?).</small>','(?<url>.+?)','', '',fpath\);");
        Match matchResults = regexObj1.Match(resultString);
        while (matchResults.Success)
        {
          // matched text: matchResults.Value
          // match start: matchResults.Index
          // match length: matchResults.Length
          cats.Add(new Category()
          {
            Id = matchResults.Groups["id"].Value,
            Name = matchResults.Groups["name"].Value,
            Number = matchResults.Groups["nr"].Value,
            Url = matchResults.Groups["url"].Value,
            PId = matchResults.Groups["pid"].Value
          });
          matchResults = matchResults.NextMatch();
        }
      }
      catch (ArgumentException ex)
      {
        // Syntax error in the regular expression
      }
      cats.Add(new Category()
      {
        Id = "-1",
        Name = "Recently Added Extensions",
        Number = "",
        Url = "/extensions/new-listing",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-2",
        Name = "Recently Updated Extensions",
        Number = "",
        Url = "/extensions/recently-updated",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-3",
        Name = "Most Installed Extensions ",
        Number = "",
        Url = "/extensions/most-favoured",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-4",
        Name = "Featured Extensions",
        Number = "",
        Url = "/extensions/featured-listing",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-5",
        Name = "Most Popular Extensions",
        Number = "",
        Url = "/extensions/popular-listing-2",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-6",
        Name = "Most Rated Extensions",
        Number = "",
        Url = "/extensions/most-rated",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-7",
        Name = "Top Rated Extensions",
        Number = "",
        Url = "/extensions/top-rated",
        PId = "0"
      });
      cats.Add(new Category()
      {
        Id = "-8",
        Name = "Most Reviewed Extensions",
        Number = "",
        Url = "/extensions/most-reviewed",
        PId = "0"
      });

    }

    public void LoadItems(Category category)
    {
      WebClient client = new WebClient();
      string site = client.DownloadString("http://www.team-mediaportal.com" + category.Url);
      //string resultString = null;
      Regex regexObj = new Regex("<div class=\"listing-summary(.*?)</div></div></div>", RegexOptions.Singleline);
      category.SiteItems.Clear();
      //resultString = regexObj.Match(site).Groups[1].Value;
      Match match = regexObj.Match(site);
      try
      {
        while (match.Success)
        {
          string item = match.Groups[0].Value;
          Regex regexObj1 = new Regex("<h3><a href=\"(?<url>.*?)\".>(?<name>.*?)</a>.*?<p.*?</a>(?<desc>.*?)</p>.*?<div class=\"fields\">(?<fields>.*?)</div></div></div>",
                                      RegexOptions.Singleline);
          Match matchResults = regexObj1.Match(item);
          while (matchResults.Success)
          {
            SiteItems items = new SiteItems()
                                {
                                  Name = matchResults.Groups["name"].Value,
                                  Descriptions = matchResults.Groups["desc"].Value,
                                  Url = matchResults.Groups["url"].Value,
                                  LogoUrl =
                                    Regex.Match(item, "<p.*?src=\"(?<img>.*?)\"", RegexOptions.Singleline).Groups["img"]
                                    .Value,
                                  EditorPick = matchResults.Value.Contains("status_editorpick.png"),
                                  Popular = matchResults.Value.Contains("status_popular.png"),
                                  JustAdded = matchResults.Value.Contains("status_new.png")
                                };
            items.LoadFields(matchResults.Groups["fields"].Value);
            category.SiteItems.Add(items);
            matchResults = matchResults.NextMatch();
          }
          match = match.NextMatch();
        }
      }
      catch (ArgumentException ex)
      {
        // Syntax error in the regular expression
      }
    }

    public List<Category> GetCats(string pid)
    {
      List<Category> list = new List<Category>();
      foreach (Category category in cats)
      {
        if(category.PId==pid)
          list.Add(category);
      }
      return list;
    }

    public Category GetCat(string id)
    {
      foreach (Category category in cats)
      {
        if (category.Id == id)
          return category;
      }
      return null;
    }

  }
}
