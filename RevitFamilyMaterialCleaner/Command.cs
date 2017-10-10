#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
#endregion

namespace RevitFamilyMaterialCleaner
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            List<string> familyFiles;
            Family modifiedFamily = null;

            if (GetFamilyFolder() == null)
            {
                TaskDialog.Show("No Folder", "No folder has been selected.");
                return Result.Cancelled;
            }
            else
            {
                familyFiles = GetFamilyFiles(GetFamilyFolder());
            }

            if (familyFiles.Count > 0)
            {
                foreach (string familyFile in familyFiles)
                {
                    modifiedFamily = GetFamily(doc, familyFile);
                    if (modifiedFamily != null)
                    {
                        DeleteSpareFamilyMaterials(doc, modifiedFamily);
                    }
                }
                TaskDialog.Show("End", "Task completed");
                return Result.Succeeded;
            }

            else
            {
                TaskDialog.Show("No FamilyFiles", "No family in selected folder.");
                return Result.Cancelled;
            }
        }       

        private string GetFamilyFolder()
        {
            TaskDialog.Show("Family folder", "Please select folder with Families.");
            string familiesFolder = string.Empty;
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog vfd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            bool? result = vfd.ShowDialog();

            if (result == true)
            {
                familiesFolder = vfd.SelectedPath;
            }
            else
            {
                var dialogResult = TaskDialog.Show("No folder selected", "Please select folder.", TaskDialogCommonButtons.Cancel | TaskDialogCommonButtons.Ok);
                if (dialogResult == TaskDialogResult.Ok)
                    familiesFolder = GetFamilyFolder();
                else
                    familiesFolder = null;
            }

            return familiesFolder;
        }

        private List<string> GetFamilyFiles(string path)
        {
            List<String> familyfiles = new List<string>();
            foreach (var item in Directory.GetFiles(path))
            {
                if (item.Contains(".rfa"))
                familyfiles.Add(item);
            }
            return familyfiles;
        }

        private Family GetFamily(Document doc, string familyFile)
        {
            Family modifiedFamily = null;
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Material cleaner");
                if (doc.LoadFamily(familyFile, out modifiedFamily))
                {
                    string materials = string.Empty;
                }
                tx.Commit();
            }
            return modifiedFamily;
        }

        private List<Material> GetFamilyTypesMaterials(Document doc, Family modifiedFamily)
        {
            List<Material> typesMaterials = new List<Material>();

            Document familyDoc = doc.EditFamily(modifiedFamily);
            FamilyManager familyManager = familyDoc.FamilyManager;
            FamilyTypeSet familyTypes = familyManager.Types;
            FamilyTypeSetIterator iterator = familyTypes.ForwardIterator();

            iterator.Reset();
            string types = string.Empty;

            using (Transaction trans = new Transaction(familyDoc, "MaterialParameter"))
            {
                trans.Start();
                while (iterator.MoveNext())
                {
                    familyManager.CurrentType = iterator.Current as FamilyType;
                    FamilyType type = familyManager.CurrentType;
                    string param = string.Empty;
                    foreach (FamilyParameter parameter in familyManager.Parameters)
                    {
                        if (parameter.Definition.Name.Contains("Mat"))
                        {
                            Material material = doc.GetElement(type.AsElementId(parameter)) as Material;
                            if (material != null)
                            typesMaterials.Add(material);
                        }
                    }
                }
                trans.Commit();
            }

            return typesMaterials;
        }

        private List<Material> GetDistinctMaterials(List<Material> typesMaterials)
        {
            List<Material> sortedMaterials = new List<Material>();
            sortedMaterials = typesMaterials.OrderBy(Object => Object.Name).ToList();

            List<Material> distinctMaterials = new List<Material>();
            Material current = sortedMaterials[0];
            distinctMaterials.Add(current);
            foreach (var item in sortedMaterials)
            {
                if (current.Id != item.Id)
                {
                    current = item;
                    distinctMaterials.Add(item);
                }
            }

            return distinctMaterials;
        }

        private List<Material> GetDocumentMaterials(Document doc)
        {
            List<Material> docMaterials = new List<Material>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Material));

            var list = collector.ToList();
            foreach (var item in list)
            {
                Material material = item as Material;
                docMaterials.Add(material);
            }

            return docMaterials;
        }

        private void PrintMaterials(List<Material> list)
        {
            string tMaterial = string.Empty;
            foreach (var item in list)
            {
                tMaterial += item.Id + ". " + item.Name + "\n";
            }
            TaskDialog.Show("Material", tMaterial);
        }

        private void DeleteMaterials(Document doc, List<Material> documentMaterials, List<Material> distinctMaterials)
        {
            documentMaterials.RemoveAll(x => distinctMaterials.Any(y => y.Name == x.Name));

            using (Transaction trans = new Transaction(doc, "DeleteMaterials"))
            {
                trans.Start();
                List<ElementId> delList = new List<ElementId>();
                foreach (var item in documentMaterials)
                {
                    ElementId elem = item.Id as ElementId;
                    delList.Add(elem);
                }

                doc.Delete(delList);
                trans.Commit();
            }            
        }

        private void DeleteSpareFamilyMaterials(Document doc, Family modifiedFamily)
        {
            Document familyDoc = doc.EditFamily(modifiedFamily);
            List<Material> typeMaterials = GetFamilyTypesMaterials(doc, modifiedFamily);
            DeleteMaterials(familyDoc, GetDocumentMaterials(familyDoc), typeMaterials);
            familyDoc.Save();
        }
    }
}
