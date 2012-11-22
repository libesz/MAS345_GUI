using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MAS345_GUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

[AttributeUsage(AttributeTargets.Assembly)]
public class AssemblyGitBuild : Attribute
{
    public string gitBuild { get; private set; }
    public AssemblyGitBuild(string txt) { gitBuild = txt; }
}
