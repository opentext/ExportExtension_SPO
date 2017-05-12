using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ExportExtensionCommon;
using DOKuStar.Diagnostics.Tracing;

namespace CaptureCenter.SPO
{
    public class SPOExport : SIEEExport
    {
        private SPOSettings mySettings;
        private ISPOClient spoClient;

        public SPOExport(ISPOClient spoClient)
        {
            this.spoClient = spoClient;
        }

        public override void Init(SIEESettings settings)
        {
            base.Init(settings);
            mySettings = settings as SPOSettings;
            mySettings.InitializeSPOClient(spoClient);
            spoClient.Login();
        }

        public override void ExportDocument(SIEESettings settings, SIEEDocument document, string targetFileName, SIEEFieldlist fieldlist)
        {
            List<SPOField> fields = new List<SPOField>();
            foreach (SIEEField f in fieldlist)
            {
                SPOField spof = mySettings.Fields.Where(n => n.InternalName == f.ExternalId).FirstOrDefault();
                if (spof != null)
                {
                    spof.Value = f.Value;
                    fields.Add(spof);
                }
            }
            spoClient.AddDocument(
                mySettings.SelectedList,
                getRelativePath(fieldlist) + targetFileName + ".pdf",
                document.PDFFileName,
                fields);
        }

        public string getRelativePath(SIEEFieldlist fieldlist)
        {
            switch (mySettings.FolderHandling)
            {
                case SPOSettings.FolderHandlingType.Folder:
                    return normalizePath(mySettings.FolderName);
                case SPOSettings.FolderHandlingType.Field:
                    return normalizePath(fieldlist.Where(n => n.Name == mySettings.FieldName).First().Value);
                case SPOSettings.FolderHandlingType.Auto:
                    return normalizePath(getAutoPath());
                case SPOSettings.FolderHandlingType.None:
                default: return string.Empty;
            }
        }

        private string normalizePath(string path)
        {
            if (path == string.Empty) return path;
            if (path[0] == '/') path = path.Substring(1, path.Length - 1);
            if (path == string.Empty) return path;
            if (path[path.Length - 1] == '/') return path;
            return path + "/";
        }

        #region Autofolder
        private string baseFolder = null;
        int currNumber = 0;
        private string getAutoPath()
        {
            switch (mySettings.SelectedAutoFolderType)
            {
                case SPOSettings.AutoFolderType.ByCapacity:
                    if (baseFolder == null)
                        findInitialFolder(mySettings.BasefolderName);
                    return getAutoFolder(mySettings.MaxCapacity);

                case SPOSettings.AutoFolderType.ByDay:
                    return handleDateAuto(DateTime.Now.ToString("yyyy-MM-dd"));

                case SPOSettings.AutoFolderType.ByDayMonthYear:
                default:
                    return handleDateAuto(DateTime.Now.ToString("yyyy-MM-dd").Replace('-', '/'));
            }
        }

        private string handleDateAuto(string basename)
        {
            if (baseFolder == null)
                if (!mySettings.ControlLoad) return basename;
                else
                    findInitialFolder(basename);
            return getAutoFolder(mySettings.MaxDay);
        }

        private void findInitialFolder(string basename)
        {
            this.baseFolder = basename;
            DocumentNameFindNumber dnfn = new DocumentNameFindNumber(FolderProbe);
            this.currNumber = dnfn.GetNextFileName(basename) - 1;
        }

        private bool FolderProbe(string foldername, int number)
        {
            return spoClient.FolderExists(mySettings.SelectedList, createFolderName(foldername, number));
        }

        private string createFolderName(string basename, int number)
        {
            return string.Format("{0}_{1:D4}", basename, number);
        }

        private string getAutoFolder(int max)
        {
            if (max== -1 || max <= spoClient.GetFolderCount(mySettings.SelectedList, currFolder()))
                currNumber++;
            return currFolder();
        }

        private string currFolder()
        {
            return createFolderName(baseFolder, currNumber);
        }
        #endregion
    }
}
