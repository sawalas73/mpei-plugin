using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MPEIPlugin.Classes;
using MPEIPlugin.MPSite;

namespace test
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      ExtensionSettings sett = new ExtensionSettings();
      sett.Load(@"e:\Documents and Settings\All Users\Application Data\Team MediaPortal\MediaPortal\Installer\V2\71cc3381-de92-482d-9556-7e86f67f8067\0.6.1.0\extension_settings.xml");
    }

    private void button2_Click(object sender, EventArgs e)
    {
      MPSiteUtil siteUtil = new MPSiteUtil();
      siteUtil.LoadCatTree();
      List<Category> cats = siteUtil.GetCats("0");
    }
  }
}
