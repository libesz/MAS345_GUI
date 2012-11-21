using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MAS345_GUI
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void AboutBox_Load(object sender, EventArgs e)
        {
            String gitBuild = "unknown";

            try
            {
                gitBuild = typeof(AssemblyGitBuild).Assembly.GetCustomAttributes(typeof(AssemblyGitBuild), false).Cast<AssemblyGitBuild>().First().gitBuild;
            }
            catch
            {
                gitBuild = "falied";
            }
            WebClient c = new WebClient();
            StringBuilder data = new StringBuilder(c.DownloadString("https://api.github.com/repos/libesz/MAS345_GUI/commits?sha=master&per_page=1"));
            data[0] = ' ';
            data[data.Length - 1] = ' ';
            JObject o = JObject.Parse(data.ToString());
            label1.Text = "Version " + Application.ProductVersion + " Build " + gitBuild + " github headrev: " + o["sha"];

        }
    }
}
