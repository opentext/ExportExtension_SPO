using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    [TestClass]
    public class TestSPOAdapter
    {
        public int connectionTab = 0;
        public int listTab = 1;

        public TestSPOAdapter()
        {
            SIEEMessageBox.Suppress = true;
        }

        #region Connection tab
        [TestMethod]
        [TestCategory("SharePoint adapter")]
        public void t01_Connection()
        {
            SPOViewModel vm = createViewModel();

            // Test initial values
            Assert.AreEqual("<http://servername/sitename>", vm.CT.SiteUrl);
            Assert.AreEqual(string.Empty, vm.CT.Username);
            Assert.AreEqual(string.Empty, vm.CT.Password);
            Assert.IsTrue(vm.CT.Office365);
            Assert.IsFalse(vm.SPOSettings.LoginPossible);
            Assert.IsFalse(vm.DataLoaded);
            Assert.AreEqual(connectionTab, vm.SelectedTab);

            // Test faild login
            vm.CT.Username = "illegal";
            SIEEMessageBox.LastMessage = null;
            vm.LoginButtonHandler();
            Assert.IsNotNull(SIEEMessageBox.LastMessage);
            Assert.IsFalse(vm.SPOSettings.LoginPossible);
            Assert.IsFalse(vm.DataLoaded);
            Assert.AreEqual(connectionTab, vm.SelectedTab);

            // Test successful login
            vm.CT.Username = "ok";
            SIEEMessageBox.LastMessage = null;
            vm.LoginButtonHandler();
            Assert.IsNull(SIEEMessageBox.LastMessage);
            Assert.IsTrue(vm.SPOSettings.LoginPossible);
            Assert.AreEqual(listTab, vm.SelectedTab);
            vm.LT.ActivateTab();
            Assert.IsTrue(vm.DataLoaded);

            // Reload settings and verify
            vm = createViewModel(vm.SPOSettings);
            Assert.IsTrue(vm.SPOSettings.LoginPossible);
            Assert.AreEqual(listTab, vm.SelectedTab);
            vm.LT.ActivateTab();
            Assert.IsTrue(vm.DataLoaded);

            // Switching tabs should not change status of Dataloded
            vm.SelectedTab = connectionTab;
            vm.LT.ActivateTab();
            Assert.IsTrue(vm.DataLoaded);

            // Set illegal password again
            vm.CT.Username = "illegal";
            Assert.IsFalse(vm.SPOSettings.LoginPossible);
            Assert.IsFalse(vm.DataLoaded);
            vm.LoginButtonHandler();
            Assert.IsFalse(vm.SPOSettings.LoginPossible);
            Assert.IsFalse(vm.DataLoaded);
            // Assert.AreEqual(connectionTab, vm.SelectedTab); Can't happen at the UI. User must be on connection tab.

            // Verify case where an account becomes invalid
            vm.CT.Username = "ok";
            vm.LoginButtonHandler();
            Assert.IsTrue(vm.SPOSettings.LoginPossible);
            Assert.AreEqual(listTab, vm.SelectedTab);
            vm.LT.ActivateTab();
            Assert.IsTrue(vm.DataLoaded);
            vm.CT.Username = "illegal";
            vm = createViewModel(vm.SPOSettings);
            Assert.IsFalse(vm.SPOSettings.LoginPossible);
            Assert.IsFalse(vm.DataLoaded);
            Assert.AreEqual(connectionTab, vm.SelectedTab);
        }
        #endregion

        #region List handling
        [TestMethod]
        [TestCategory("SharePoint adapter")]
        public void t02_ListHandling()
        {
            SPOViewModel vm = createViewModel();

            // Initial values
            Assert.IsNull(vm.LT.Lists);
            Assert.IsNull(vm.LT.SelectedList);

            List<SPOList> testLists = vm.SPOClient.GetLists();

            // First login: Default selected item
            vm.LoginButtonHandler();
            vm.SPOSettings.ListFilter.TypeTemplateRanges.Add(new SPOListFilter.TypeTemplateRange() { From = 5000, To = 5005, });
            vm.LT.ActivateTab();
            Assert.AreEqual(testLists.Count(), vm.LT.Lists.Count());
            Assert.AreEqual(testLists[0].Id, vm.LT.SelectedList.Id);

            // Activate tab with preset SelectedList
            vm.LT.SelectedList = testLists[1];
            vm.LT.Lists = null;
            vm.DataLoaded = false;
            vm.LT.ActivateTab();
            Assert.AreEqual(testLists.Count(), vm.LT.Lists.Count());
            Assert.AreEqual(testLists[1].Id, vm.LT.SelectedList.Id);

            // Activate tab with preset but not-existing SelectedList
            vm.LT.SelectedList = new SPOList() { Id = Guid.NewGuid() };
            vm.LT.Lists = null;
            vm.DataLoaded = false;
            vm.LT.ActivateTab();
            Assert.AreEqual(testLists.Count(), vm.LT.Lists.Count());
            Assert.AreEqual(testLists[0].Id, vm.LT.SelectedList.Id);

            // Activate tab with empty list of Lists
            vm.LT.SelectedList = testLists[1];
            vm.LT.Lists = null;
            vm.DataLoaded = false;
            ((SPOClientMock)vm.SPOClient).ReturnEmptyList = true;
            vm.LT.ActivateTab();
            Assert.AreEqual(0, vm.LT.Lists.Count());
            Assert.AreEqual(testLists[1].Id, vm.LT.SelectedList.Id); // unchanged
            ((SPOClientMock)vm.SPOClient).ReturnEmptyList = false;
        }
        #endregion

        #region Field handling
        [TestMethod]
        [TestCategory("SharePoint adapter")]
        public void t03_FieldHandling()
        {
            SPOViewModel vm = createViewModel();
            List<SPOField> testFields = vm.SPOClient.GetFields(new SPOList());
            Assert.AreEqual(testFields.Count(), testFields.Where(n => !n.Use).Count());

            // Initial values
            Assert.AreEqual(0, vm.LT.Fields.Count);

            // Get all fields
            vm.LoadFieldsButtonHandler();
            Assert.AreEqual(testFields.Count(), vm.LT.Fields.Count);

            foreach (SPOField f in vm.LT.Fields)
                Assert.AreEqual(
                    f.Use ? "use" : "not", 
                    testFields.Where(n => n.Title == f.Title).First().DefaultValue
                );

            Assert.AreEqual(2, vm.LT.Fields.Where(n => n.Use).Count());
        }
        #endregion

        #region Export
        [TestMethod]
        [TestCategory("SharePoint adapter")]
        public void t04_Export()
        {
            SPOViewModel vm = createViewModel();

            vm.LT.SelectedList = new SPOList() { Title = "SomeList" };
            vm.LoadFieldsButtonHandler();
            SIEEFieldlist schema = vm.Settings.CreateSchema();
            Assert.AreEqual(2, schema.Count);

            schema.Where(n => n.ExternalId == "Id1").First().Value = "myTitle";
            schema.Where(n => n.ExternalId == "Id3").First().Value = "true";
            schema.Add(new SIEEField() { Name = "SomeName", ExternalId = "xx", Value = "SomeValue" });

            SPOClientMock spoClient = new SPOClientMock();
            SPOExport export = new SPOExport(spoClient);
            export.Init(vm.Settings);

            SIEEDocument document = new SIEEDocument() { PDFFileName = "file.pdf" };
            export.ExportDocument(vm.Settings, document, "sub/Document", schema);

            Assert.AreEqual("SomeList", spoClient.LastExportResult.ListTitle);
            Assert.AreEqual("sub/Document.pdf", spoClient.LastExportResult.DocumentPath);
            Assert.AreEqual("file.pdf", spoClient.LastExportResult.FilePath);
            verifyField("Id1", "myTitle", schema, spoClient.LastExportResult.Fields);
            verifyField("Id3", "true", schema, spoClient.LastExportResult.Fields);
        }

        private void verifyField(string id, string value, SIEEFieldlist fl, List<SPOField> spofl)
        {
            Assert.AreEqual(
                fl.Where(n => n.ExternalId == id).First().Value,
                spofl.Where(n => n.InternalName == id).First().Value
            );
        }
        #endregion

        #region Subfolder handling
        [TestMethod]
        [TestCategory("SharePoint adapter")]
        public void t05_SubfolderHandling()
        {
            SPOViewModel vm = createViewModel();

            vm.LT.SelectedList = new SPOList() { Title = "SomeList" };
            vm.LoadFieldsButtonHandler();
            SIEEFieldlist schema = vm.Settings.CreateSchema();
            Assert.AreEqual(2, schema.Count);

            schema.Where(n => n.ExternalId == "Id1").First().Value = "myTitle";
            schema.Where(n => n.ExternalId == "Id3").First().Value = "true";
            schema.Add(new SIEEField() { Name = "SomeName", ExternalId = "xx", Value = "SomeValue" });

            SIEEDocument document = new SIEEDocument() { PDFFileName = "file.pdf" };

            SPOClientMock spoClient = new SPOClientMock();
            SPOExport export = new SPOExport(spoClient);
            string documentName = "abc/document";

            var td = new[]
            {
                new {
                    n = 0, folderType = SPOSettings.FolderHandlingType.None,
                    folder = string.Empty, field = false,
                    fieldContent = string.Empty,
                    result = documentName, },
                new {
                    n = 1, folderType = SPOSettings.FolderHandlingType.Folder,
                    folder = "sub/subsub", field = false,
                    fieldContent = string.Empty,
                    result = "sub/subsub/" + documentName, },
                new {
                    n = 2, folderType = SPOSettings.FolderHandlingType.Folder,
                    folder = "/sub/subsub/", field = false,
                    fieldContent = string.Empty,
                    result = "sub/subsub/" + documentName, },
                new {
                    n = 3, folderType = SPOSettings.FolderHandlingType.Folder,
                    folder = "/", field = false,
                    fieldContent = string.Empty,
                    result = documentName, },
                new {
                    n = 4, folderType = SPOSettings.FolderHandlingType.Field,
                    folder = string.Empty, field = true,
                    fieldContent = "sub/subsub",
                    result = "sub/subsub/" + documentName, },
                new {
                    n = 5, folderType = SPOSettings.FolderHandlingType.Field,
                    folder = string.Empty, field = true,
                    fieldContent = "/sub/subsub/",
                    result = "sub/subsub/" + documentName, },
                new {
                    n = 6, folderType = SPOSettings.FolderHandlingType.Field,
                    folder = string.Empty, field = true,
                    fieldContent = "/",
                    result = documentName, },
            };
            int doOnly = -1;
            for (int i = 0; i != td.Length; i++)
            {
                if (doOnly > 0 && td[i].n != doOnly) continue;

                vm.FT.FolderHandling = SPOSettings.FolderHandlingType.None;
                vm.FT.FolderName = td[i].folder;
                vm.FT.FieldName = string.Empty;
                if (td[i].field)
                {
                    vm.FT.FieldName = "SomeName";
                    schema.Where(n => n.Name == "SomeName").First().Value = td[i].fieldContent;
                }
                vm.FT.FolderHandling = td[i].folderType;

                export.Init(vm.Settings);
                export.ExportDocument(vm.Settings, document, documentName, schema);
                Assert.AreEqual(td[i].result + ".pdf", spoClient.LastExportResult.DocumentPath);
            }
            Assert.IsTrue(doOnly < 0, "Not all tests executed");
        }

        #endregion

        #region Autofolders
        [TestMethod]
        [TestCategory("SharePoint adapter")]
        public void t06_AutoFolders()
        {
            SPOViewModel vm = createViewModel();

            // Verify initial values
            Assert.AreEqual(SPOSettings.AutoFolderType.ByCapacity, vm.FT.SelectedAutoFolderType);
            Assert.AreEqual("Document", vm.FT.BasefolderName);
            Assert.AreEqual(2000, vm.FT.MaxCapacity);
            Assert.IsFalse(vm.FT.ControlLoad);
            Assert.AreEqual(2000, vm.FT.MaxDay);

            SPOClientMock spoClient;

            vm = t06_createViewModel(SPOSettings.AutoFolderType.ByCapacity);
            spoClient = new SPOClientMock();
            t06_testExport(vm, spoClient, new SPOExport(spoClient), "Document_0000");

            vm = t06_createViewModel(SPOSettings.AutoFolderType.ByCapacity);
            t06_sequence(vm, "Document", 21, 3, true);

            // Avoid sporadic errors around datum change (midnight)
            if (!(DateTime.Now.Hour < 23 || DateTime.Now.Minute < 45)) return;

            vm = t06_createViewModel(SPOSettings.AutoFolderType.ByDay);
            string now = DateTime.Now.ToString("yyyy-MM-dd");
            spoClient = new SPOClientMock();
            t06_testExport(vm, spoClient, new SPOExport(spoClient), now);

            vm = t06_createViewModel(SPOSettings.AutoFolderType.ByDay);
            vm.FT.ControlLoad = true;
            t06_sequence(vm, now, 866, 42, false);

            vm = t06_createViewModel(SPOSettings.AutoFolderType.ByDayMonthYear);
            now = DateTime.Now.ToString("yyyy-MM-dd").Replace('-', '/');
            spoClient = new SPOClientMock();
            t06_testExport(vm, spoClient, new SPOExport(spoClient), now);

            vm = t06_createViewModel(SPOSettings.AutoFolderType.ByDayMonthYear);
            vm.FT.ControlLoad = true;
            t06_sequence(vm,now, 21, 3, false);
        }

        private void t06_sequence(SPOViewModel vm, string basename, int maxFolder, int maxFile, bool folderFull)
        {
            int add = folderFull ? 1 : 0;
            SPOClientMock spoClient = new SPOClientMock();
            spoClient.MaxExistingFolderNumer = maxFolder;
            vm.FT.MaxCapacity = maxFile;
            SPOExport export = new SPOExport(spoClient);
            for (int i = 0; i < maxFile; i++)
                t06_testExport(vm, spoClient, export, string.Format("{0}_{1:D4}", basename, maxFolder));
            t06_testExport(vm, spoClient, export, string.Format("{0}_{1:D4}", basename, maxFolder + add));
        }

        private SPOViewModel t06_createViewModel(SPOSettings.AutoFolderType type, int max = 3)
        {
            SPOViewModel vm = createViewModel();
            vm.FT.FolderHandling = SPOSettings.FolderHandlingType.Auto;
            vm.FT.SelectedAutoFolderType = type;
            vm.FT.MaxCapacity = max;
            vm.FT.MaxCapacity = max;
            vm.FT.BasefolderName = "Document";
            vm.LT.SelectedList = new SPOList() { Title = "SomeList" };
            vm.LoadFieldsButtonHandler();
            return vm;
        }

        private void t06_testExport
            (SPOViewModel vm, 
            SPOClientMock spoClient, 
            SPOExport export, string expectedFolder)
        {
            SIEEDocument document = new SIEEDocument() { PDFFileName = "file.pdf" };
            string documentName = "document";
            export.Init(vm.Settings);
            export.ExportDocument(vm.Settings, document, documentName, vm.Settings.CreateSchema());
            Assert.AreEqual(
                spoClient.LastExportResult.Folder, 
                expectedFolder);
        }
        #endregion

        #region Utilities
        private SPOViewModel createViewModel(SPOSettings settings = null)
        {
            if (settings == null) settings = new SPOSettings();
            SPOViewModel vm = new SPOViewModel(settings, new SPOClientMock());
            vm.Initialize(new SPOControlWPF());
            return vm;
        }
        #endregion
    }
}
