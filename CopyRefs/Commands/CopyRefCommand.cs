using System;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using EnvDTE;
using System.Windows.Forms;
using System.Diagnostics;
using VSLangProj;
using System.IO;
using System.Collections.Generic;

namespace CopyRefs
{
    internal sealed class CopyRefCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("a55f8b39-24e7-47ef-85ab-4b95cad93869");
        Microsoft.VisualStudio.Shell.Package package;
        public static CopyRefCommand Instance { get; private set; }
        IServiceProvider ServiceProvider { get { return this.package; } }

        public static void Initialize(Microsoft.VisualStudio.Shell.Package package)
        {
            Instance = new CopyRefCommand(package);
        }

        CopyRefCommand(Microsoft.VisualStudio.Shell.Package package)
        {
            this.package = package;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.OnMenuInvoked, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        void OnMenuInvoked(object sender, EventArgs e)
        {
            string[] selectedRefs = Global.Dte.SelectedItems
                                              .Cast<SelectedItem>()
                                              .Select(x => x.Name)
                                              .ToArray();

            Project sourceProject = Global.Dte.GetSelectedProject();
            if (sourceProject == null)
                return;

            try
            {
                string[] refs = sourceProject.References()
                                             .Where(x => x.Name.IsIn(selectedRefs))
                                             .Select(x =>
                                              {
                                                  string refDescription = x.Description;

                                                  if (string.IsNullOrEmpty(refDescription))
                                                  {
                                                      var projectFile = x.ContainingProject.FileName;
                                                      return "copyref:proj:" + projectFile;
                                                  }
                                                  else
                                                  {
                                                      bool pathRef = x.CopyLocal;

                                                      if (Path.IsPathRooted(refDescription) || pathRef)
                                                          return "copyref:file:" + x.Path;
                                                      else
                                                          return "copyref:gac:" + Path.GetFileNameWithoutExtension(refDescription);
                                                  }
                                              })
                                             .ToArray();

                var text = string.Join("\n", refs);
                Clipboard.SetText(text);
            }
            catch { } //expected to fail if project is not fully initialized
        }
    }
}