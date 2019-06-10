using System;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using EnvDTE;
using VSLangProj;
using System.Collections.Generic;
using System.Globalization;

// Remove duplicates
// https://gist.github.com/AnasFullStack/b9eba91cff214427d424fe6747a77987

namespace CopyRefs
{
    internal sealed class PasteRefCommand
    {
        public const int CommandId = 256;
        public static readonly Guid CommandSet = new Guid("b0bc46a9-efb9-4f72-a44d-ef6067e4d8f1");
        readonly Microsoft.VisualStudio.Shell.Package package;
        public static PasteRefCommand Instance { get; private set; }
        IServiceProvider ServiceProvider { get { return this.package; } }

        public static void Initialize(Microsoft.VisualStudio.Shell.Package package)
        {
            Instance = new PasteRefCommand(package);
        }

        private PasteRefCommand(Microsoft.VisualStudio.Shell.Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(OnMenuInvoked, menuCommandID);
                menuItem.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        // Great help from "http://www.diaryofaninja.com/blog/2014/02/18/who-said-building-visual-studio-extensions-was-hard"
        void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            menuCommand.Visible = true;
            menuCommand.Enabled = true;
        }

        void OnMenuInvoked(object sender, EventArgs e)
        {
            try
            {
                var text = Clipboard.GetText();

                if (!text.StartsWithAny("copyref:file:", "copyref:gac:", "copyref:proj:"))
                    return;

                Project destProject = Global.Dte.GetSelectedProject();
                if (destProject == null)
                    return;

                //In Visual all projects contain an implied reference to System.Core, even if System.Core is removed from the list of references.
                //https://msdn.microsoft.com/en-us/library/wkze6zky.aspx
                try
                {
                    foreach (var line in text.Split('\n'))
                    {
                        try
                        {
                            if (line.StartsWith("copyref:file:"))
                            {
                                string path = line.Substring("copyref:file:".Length).Trim();
                                destProject.AddReference(path);
                            }
                            else if (line.StartsWith("copyref:gac:"))
                            {
                                string path = line.Substring("copyref:gac:".Length).Trim();
                                destProject.AddReference(path);
                            }
                            else if (line.StartsWith("copyref:proj:"))
                            {
                                string path = line.Substring("copyref:proj:".Length).Trim();
                                var proj = Global.Dte.GetProjectByFile(path);
                                try
                                {
                                    destProject.AddReference(proj, throwOnError: true);
                                }
                                catch (Exception ex)
                                {
                                    VsShellUtilities.ShowMessageBox(
                                        this.ServiceProvider,
                                        ex.Message,
                                        null,
                                        OLEMSGICON.OLEMSGICON_INFO,
                                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { } //expected to fail if project is not fully initialized
            }
            catch { }
        }
    }
}