using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Globalization;
using System.Windows.Forms;
using ExportExtensionCommon;
using DOKuStar.Diagnostics.Tracing;

namespace CaptureCenter.SPO
{
    [Serializable]
    public class SPOSettings : SIEESettings
    {
        #region Contruction
        public SPOSettings()
        {
            // Connection 
            SiteUrl = "<http://servername/sitename>";
            Username = Password = "";
            Office365 = true;
            loginPossible = false;

            // Lists
            Fields = new List<SPOField>();
            ListFilter = getDefaultFilter();
            FolderName = "<Enter name of folder>";
            FieldName = "<Enter name of OCC field>";
            SelectedCultureInfoName = CultureInfo.CurrentCulture.Name;

            // Folder
            FolderHandling = FolderHandlingType.None;
            SelectedAutoFolderType = AutoFolderType.ByCapacity;
            ControlLoad = false;
            MaxCapacity = 2000;
            basefolderName = "Document";
            MaxDay = 2000;

            // Document name
            UseSpecification = true;
            Specification = "<BATCHID>_<DOCUMENTNUMBER>";
        }
        #endregion

        #region Properties Connection tab
        private string siteUrl;
        public string SiteUrl
        {
            get { return siteUrl; }
            set { SetField(ref siteUrl, value); }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set { SetField(ref username, value); }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { SetField(ref password, value); ; }
        }

        private bool office365;
        public bool Office365
        {
            get { return office365; }
            set { SetField(ref office365, value); ; }
        }

        private bool loginPossible;
        public bool LoginPossible
        {
            get { return loginPossible; }
            set { SetField(ref loginPossible, value); }
        }
        #endregion

        #region Properties List tab
        private SPOList selectedList;
        public SPOList SelectedList
        {
            get { return selectedList; }
            set { SetField(ref selectedList, value); }
        }

        private List<SPOField> fields;
        public List<SPOField> Fields
        {
            get { return fields; }
            set { SetField(ref fields, value); }
        }

        private SPOListFilter listFilter;
        public SPOListFilter ListFilter
        {
            get { return listFilter; }
            set { SetField(ref listFilter, value); }
        }

        private string selectedCultureInfoName;
        public string SelectedCultureInfoName
        {
            get { return selectedCultureInfoName; }
            set { SetField(ref selectedCultureInfoName, value); }
        }
        #endregion

        #region Properties Folder tab
        public enum FolderHandlingType { None, Folder, Field, Auto };

        private FolderHandlingType folderHandling;
        public FolderHandlingType FolderHandling
        {
            get { return folderHandling; }
            set { SetField(ref folderHandling, value); }
        }

        private string fieldName;
        public string FieldName
        {
            get { return fieldName; }
            set { SetField(ref fieldName, value); }
        }

        private string folderName;
        public string FolderName
        {
            get { return folderName; }
            set { SetField(ref folderName, value); }
        }

        public enum AutoFolderType { ByDay, ByCapacity, ByDayMonthYear }

        private AutoFolderType selectedAutoFolderType;
        public AutoFolderType SelectedAutoFolderType
        {
            get { return selectedAutoFolderType; }
            set { SetField(ref selectedAutoFolderType, value); }
        }

        private bool controlLoad;
        public bool ControlLoad
        {
            get { return controlLoad; }
            set { SetField(ref controlLoad, value); }
        }

        private int maxCapacity;
        public int MaxCapacity
        {
            get { return maxCapacity; }
            set { SetField(ref maxCapacity, value); }
        }

        private string basefolderName;
        public string BasefolderName
        {
            get { return basefolderName; }
            set { SetField(ref basefolderName, value); }
        }

        private int maxDay;
        public int MaxDay
        {
            get { return maxDay; }
            set { SetField(ref maxDay, value); }
        }
        #endregion

        #region Properties Document tab
        private bool useInputFileName;
        public bool UseInputFileName
        {
            get { return useInputFileName; }
            set { SetField(ref useInputFileName, value); }
        }

        private bool useSpecification;
        public bool UseSpecification
        {
            get { return useSpecification; }
            set { SetField(ref useSpecification, value); RaisePropertyChanged(specification_Name); }
        }

        private string specification_Name = "Specification";
        private string specification;
        public string Specification
        {
            get { return useSpecification ? specification : null; }
            set { SetField(ref specification, value); }
        }
        #endregion

        #region Functions

        public void InitializeSPOClient(ISPOClient spoc)
        {
            spoc.SiteUrl = SiteUrl;
            spoc.Username = Username;
            spoc.SetPassowrd(PasswordEncryption.Decrypt(Password));
            spoc.Office365 = Office365;
            spoc.ClientCulture = new CultureInfo(SelectedCultureInfoName);
        }

        public override SIEEFieldlist CreateSchema()
        {
            SIEEFieldlist schema = new SIEEFieldlist();
            foreach (SPOField f in Fields)
                if (f.Use) schema.Add(new SIEEField { Name = f.Title, ExternalId = f.InternalName });
            return schema;
        }

        public override object Clone()
        {
            SPOSettings clone = this.MemberwiseClone() as SPOSettings;
            clone.ListFilter = this.ListFilter.Clone() as SPOListFilter;
            return clone;
        }

        public override string GetDocumentNameSpec()
        {
            return Specification;
        }
        #endregion

        #region ListFilter Serialization and initialization
        private SPOListFilter getDefaultFilter()
        {
            string defaultFilter = Properties.Settings.Default.DefaultListFilter;
            if (defaultFilter != null && defaultFilter != string.Empty)
                try
                {
                    return Serializer.DeserializeFromXmlString(defaultFilter, typeof(SPOListFilter), Encoding.Unicode) as SPOListFilter;
                }
                catch (Exception e) {
                    SIEEMessageBox.Show(
                        "Cannot retrieve filter settings. Loading system defaults.\n" + e.Message,
                        "Load default settings",
                        System.Windows.MessageBoxImage.Asterisk);
                }

            return new SPOListFilter()
            {
                ValidBaseTypes = new System.Collections.ObjectModel.ObservableCollection<SPOListFilter.BaseType>()
                {
                    new SPOListFilter.BaseType() { Type = 0 },
                    new SPOListFilter.BaseType() { Type = 1 },
                },
                TypeTemplateRanges = new System.Collections.ObjectModel.ObservableCollection<SPOListFilter.TypeTemplateRange>()
                {
                    new SPOListFilter.TypeTemplateRange() { From = 100, To= 101 }
                }
            };
        }

        public static void SaveAsDefaultFilter(SPOListFilter filter)
        {
            Properties.Settings.Default.DefaultListFilter =                  
                Serializer.SerializeToXmlString(filter, Encoding.Unicode);
            Properties.Settings.Default.Save();

            SIEEMessageBox.Show(
                "Done.", "Save default list filter",
                System.Windows.MessageBoxImage.Information);
        }

        public static void SaveFilterAs(SPOListFilter filter)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "Filter files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == DialogResult.OK)
                Serializer.SerializeToXmlFile(filter, dlg.FileName, Encoding.Unicode);
        }

        public static SPOListFilter LoadFilter()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Filter files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == DialogResult.OK)
                try
                {
                    return Serializer.DeserializeFromXmlFile(dlg.FileName, typeof(SPOListFilter), Encoding.Unicode) as SPOListFilter;
                }
                catch (Exception e)
                {
                    SIEEMessageBox.Show(
                        "Cannot retrieve filter settings. Loading system defaults.\n" + e.Message,
                        "Load default settings",
                        System.Windows.MessageBoxImage.Asterisk);
                }
            return null;
        }

        #endregion
    }
}