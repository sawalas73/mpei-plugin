using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.GUI.Library;
using MpeCore;

namespace MPEIPlugin
{
  public enum DownLoadItemType
  {
    IndexList,
    UpdateInfo,
    Extension,
    Logo,
    Other
  }

  public class DownLoadInfo
  {
    public DownLoadInfo()
    {
      ItemType = DownLoadItemType.Other;
    }

    public string Url { get; set; }
    public string TempFile { get; set; }
    public string Destinatiom { get; set; }
    public object Tag { get; set; }
    public DownLoadItemType ItemType { get; set; }
    public PackageClass Package { get; set; }
    public GUIListItem ListItem { get; set; }

  }
}
