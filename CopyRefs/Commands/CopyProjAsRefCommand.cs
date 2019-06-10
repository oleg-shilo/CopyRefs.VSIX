using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace CopyRefs
{
    internal sealed class CopyProjAsRefCommand
    {
        public const int CommandId = 256;
        public static readonly Guid CommandSet = new Guid("52c01e0f-9f16-493c-aac9-68e0103a0121");
        public static CopyProjAsRefCommand Instance { get; private set; }
        IServiceProvider ServiceProvider;

        public static void Initialize(Package package)
        {
            Instance = new CopyProjAsRefCommand(package);
        }

        private CopyProjAsRefCommand(AsyncPackage package)
        {
            // ServiceProvider = package;

            OleMenuCommandService commandService = package.GetServiceAsync(typeof(IMenuCommandService)).Result as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            Project sourceProject = Global.Dte.GetSelectedProject();
            if (sourceProject != null)
                try
                {
                    Clipboard.SetText("copyref:proj:" + sourceProject.FileName);
                }
                catch { }
        }
    }
}