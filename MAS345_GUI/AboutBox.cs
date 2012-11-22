using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace MAS345_GUI
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        public static void ShowDialog(string CurrentBuild, string LatestBuild)
        {
            AboutBox Instance = new AboutBox();
            Instance.versionLabel.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString()
                                         + ", build: " + CurrentBuild.Substring(0,8);
            Instance.ShowDialog();
       }

        private void gitHubLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Properties.Settings.Default.GitHubUrl);
            }
            catch
            {
                MessageBox.Show("Browser start error!");
            }
        }

        private void homePageLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Properties.Settings.Default.HomePageUrl);
            }
            catch
            {
                MessageBox.Show("Browser start error!");
            }
        }

        private void licenseLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Properties.Settings.Default.LicenseUrl);
            }
            catch
            {
                MessageBox.Show("Browser start error!");
            }
        }
    }
}
