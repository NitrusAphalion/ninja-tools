using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NinjaTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Handle command line arguments
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            for (int i = 0; i < e.Args.Length; i++)
            {
                string[] split = e.Args[i].Split('=');
                string arg = split[0];
                string param = split[1];

                switch (arg)
                {
                    case "-p":
                        string[] list = param.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (list.Length > 0)
                        {
                            if (Pages.Helpers.TabHelpers.CmdLineTabs == null)
                                Pages.Helpers.TabHelpers.CmdLineTabs = new List<List<string>>();
                            Pages.Helpers.TabHelpers.CmdLineTabs.Add(list.ToList());
                        }
                        break;
                }
            }
        }
    }
}