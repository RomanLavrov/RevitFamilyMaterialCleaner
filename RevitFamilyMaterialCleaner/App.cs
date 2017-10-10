#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using Autodesk.Revit.UI.Events;
using System.Threading;
#endregion

namespace RevitFamilyMaterialCleaner
{
    class App : IExternalApplication
    {

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        public Result OnStartup(UIControlledApplication a)
        {
            //a.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(a_DialogBoxShowing);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }

        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            return System.Reflection.Assembly.Load(bytes);
        }

        //void a_DialogBoxShowing(  object sender,DialogBoxShowingEventArgs e)
        //{
        //    TaskDialogShowingEventArgs e2 = e as TaskDialogShowingEventArgs;

        //    string s = string.Empty;

        //    if (null != e2)
        //    {
        //        s = string.Format( ", dialog id {0}, message '{1}'", e2.DialogId, e2.Message);

        //        bool isConfirm = e2.DialogId.Equals("Progress");

        //        if (isConfirm)
        //        {
        //            Thread.Sleep(10000);
        //            e2.OverrideResult((int)TaskDialogResult.Close);

        //            s += ", auto-confirmed.";
        //        }
        //    }

        //    System.Diagnostics.Debug.Print("DialogBoxShowing: help id {0}, cancellable {1}{2}", e.HelpId, e.Cancellable ? "Yes" : "No", s);
        //}
    }
}
