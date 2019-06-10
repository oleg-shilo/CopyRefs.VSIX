using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using EnvDTE;
using System.Windows.Forms;
using System.Diagnostics;
using VSLangProj;
using System.IO;


static class Global
{
    public static DTE dte;
    public static DTE Dte
    {
        get { return dte = dte ?? Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE; }
    }

    public static Project GetSelectedProject(this DTE dte)
    {
        Project result = null;
        try
        {
            IVsProject vsProj = Global.GetSelectedVsProject();
            if (vsProj != null)
                result = dte.GetProjectByFile(vsProj.FileName());
        }
        catch { }
        return result;
    }

    public static IVsProject GetSelectedVsProject()
    {
        uint itemid = VSConstants.VSITEMID_NIL;

        var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

        if (monitorSelection == null)
            return null;

        IVsMultiItemSelect multiItemSelect = null;
        var hierarchyPtr = IntPtr.Zero;
        var selectionContainerPtr = IntPtr.Zero;

        try
        {
            int hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

            if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                return null; // there is no selection

            return Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsProject;
        }
        finally
        {
            if (selectionContainerPtr != IntPtr.Zero)
                Marshal.Release(selectionContainerPtr);

            if (hierarchyPtr != IntPtr.Zero)
                Marshal.Release(hierarchyPtr);
        }
    }

    static public bool StartsWithAny(this string text, params string[] patterns)
    {
        return patterns.Any(x => text.StartsWith(x)); 
    }

    static public Project GetProjectByFile(this DTE dte, string projectFile)
    {
        try
        {
            foreach (Project pr in dte.Solution.Projects)
                if (pr.FileName == projectFile)
                    return pr;
        }
        catch { }
        return null;
    }

    static public Project AddReference(this Project proj, string reference)
    {
        try
        {
            var vspr = proj.Object as VSProject;
            vspr.References.Add(reference);
        }
        catch { }
        return null;
    }

    static public Project AddReference(this Project proj, Project reference, bool throwOnError = false)
    {
        try
        {
            var vspr = proj.Object as VSProject;
            vspr.References.AddProject(reference);
        }
        catch 
        {
            if (throwOnError)
                throw;
        }
        return null;
    }

    static public IEnumerable<Reference> References(this Project proj)
    {
        return (proj.Object as VSProject).References.Cast<Reference>();
    }


    static public bool IsIn<T>(this T obj, IEnumerable<T> collection)
    {
        return collection.Contains(obj);
    }

    static public Project FindByFileName(this IEnumerable<Project> projects, string file)
    {
        return projects.FirstOrDefault(x => x.FileName == file);
    }

    static public string FileName(this IVsProject project)
    {
        string projectFullPath = null;
        project.GetMkDocument(VSConstants.VSITEMID_ROOT, out projectFullPath);
        return projectFullPath;
    }
}

