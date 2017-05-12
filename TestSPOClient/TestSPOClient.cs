using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Globalization;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    [TestClass]
    public class TestSPOClient
    {
        #region Test infrastucture
        /// The tests are designed to run against a set of test systems.
        /// Each test system is defined by certain properties like Url, username, etc.
        public class SPOTestSystem
        {
            public SPOTestSystem() { }
            public string TestSystemName { get; set; }
            public bool Active { get; set; } = true;
            public bool Office365 { get; set; } = true;
            public string SiteUrl { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }

            // Verification for GetLists() call
            public string ListPattern { get; set; } = "Occ";
            public int ListMin { get; set; }

            // Verificatioan for GetFields call
            public string TestLibrary { get; set; } = "OccTestLibrary";
            public int FieldMin { get; set; } = 2;
        }

        private List<SPOTestSystem> testsystems = new List<SPOTestSystem>()
        {
           new SPOTestSystem()
           {
                TestSystemName = "Default",
                Active = true,
                SiteUrl = "https://opentext.sharepoint.com/sites/occ",
                Username = "",
                Password = "",
                ListMin = 2,
            },
        };

        private string testDocument;
        private const string unitTestLibrary = "OccUnitTestLibrary";
        private const string unitTestList = "OccUnitTestList";
        private bool reallyRemoveStuff = false;

        public TestSPOClient()
        {
            testsystems = SIEEUtils.GetLocalTestDefinintions(testsystems);
            testDocument = Path.GetTempFileName().Replace(".tmp", ".pdf");
            File.WriteAllBytes(testDocument, Properties.Resources.Document);

            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active && reallyRemoveStuff)
                {
                    reallyRemoveTestLists(ts, unitTestLibrary);
                    reallyRemoveTestLists(ts, unitTestList);
                }
        }

        private void reallyRemoveTestLists(SPOTestSystem ts, string listname)
        {
            // http://stackoverflow.com/questions/42146301/deleting-list-does-not-work-list-does-not-exist

            SPOClient spoc = createClient(ts) as SPOClient;
            spoc.Login();

            try { spoc.DeleteList(listname); } catch (Exception e) { var x = e.Message; }
            spoc.CreateList(listname, false);

            spoc = createClient(ts) as SPOClient;
            spoc.Login();

            spoc.DeleteList(listname);
            spoc.Dispose();
        }
        #endregion

        #region Test and explore
        [TestMethod]
        [TestCategory("SharePoint client test")]
        /// This is not really a unit test but some place to play around.
        public void t00_TestAndExplore()
        {
            SPOClient spoc = createClient(testsystems.Where(n => n.TestSystemName == "vmsp2013").First()) as SPOClient;
            spoc.Login();

            
        }
        #endregion

        #region Login
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t01_Login()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active) t01_Login1(ts);
        }
        private void t01_Login1(SPOTestSystem ts)
        {
            // Confirm regular login
            ISPOClient spoClient = createClient(ts);
            spoClient.Login();

            // Confirm failure with wrong password
            bool gotError = false;
            spoClient.SetPassowrd(ts.Password + "_illegal");
            try { spoClient.Login(); }
            catch { gotError = true; }
            Assert.IsTrue(gotError);
        }
        #endregion

        #region Get Lists
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t02_GetLists()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active) t02_GetLists1(ts);
        }
        private void t02_GetLists1(SPOTestSystem ts)
        {
            // Get all lists
            ISPOClient spoClient = createClient(ts);
            spoClient.Login();
            List<SPOList> result = spoClient.GetLists();

            // Verify we've got enough known lists
            Regex regex = new Regex(ts.ListPattern);
            Assert.IsTrue(ts.ListMin <= result.Where(n => regex.Match(n.Title).Success).Count());

            // Verify we've got several base types and template types
            Assert.IsTrue(0 < result.Select(n => n.BaseType).Distinct().Count());
            Assert.IsTrue(0 < result.Select(n => n.TemplateType).Distinct().Count());
        }
        #endregion

        #region List filters
        /// Test the filters specify which lists are presented to the user to chose from.
        /// See filters.cs for more detail.
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t03_ListFilters()
        {
            List<SPOList> testList = new List<SPOList>()
            {
                new SPOList() { BaseType = 0, TemplateType = 100, Title = "List 1" },
                new SPOList() { BaseType = 1, TemplateType = 100, Title = "List 2" },
                new SPOList() { BaseType = 1, TemplateType = 101, Title = "Library 1" },
                new SPOList() { BaseType = 1, TemplateType = 5000, Title = "Library 2" },
                new SPOList() { BaseType = 1, TemplateType = 5005, Title = "Library 3" },
            };
            SPOListFilter listFilter = new SPOListFilter();

            // Test Base type filters
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.ValidBaseTypes.Add(new SPOListFilter.BaseType() { Type = 1 });
            Assert.AreEqual(testList.Where(n => n.BaseType == 1).Count(), listFilter.Filter(testList).Count);
            listFilter.ValidBaseTypes.Add(new SPOListFilter.BaseType() { Type = 0 });
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.ValidBaseTypes.Clear();

            // Test Template type filters
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.TypeTemplateRanges.Add(new SPOListFilter.TypeTemplateRange() { From = 100, To = 100, });
            Assert.AreEqual(testList.Where(n => n.TemplateType == 100).Count(), listFilter.Filter(testList).Count);
            listFilter.TypeTemplateRanges.Add(new SPOListFilter.TypeTemplateRange() { From = 5000, To = 5005, });
            Assert.AreEqual(4, listFilter.Filter(testList).Count);
            listFilter.TypeTemplateRanges.Add(new SPOListFilter.TypeTemplateRange() { From = 0, To = 10000, });
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.TypeTemplateRanges.Clear();

            // Test Title filters
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.TitleFilters.Add(new SPOListFilter.TitleFilter() { Pattern = "List", Include = true });
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.TitleFilters.Add(new SPOListFilter.TitleFilter() { Pattern = "Library", Include = false });
            Assert.AreEqual(2, listFilter.Filter(testList).Count);
            listFilter.TitleFilters.Clear();

            // Combinations
            Assert.AreEqual(testList.Count, listFilter.Filter(testList).Count);
            listFilter.ValidBaseTypes.Add(new SPOListFilter.BaseType() { Type = 1 });
            Assert.AreEqual(4, listFilter.Filter(testList).Count);
            listFilter.TypeTemplateRanges.Add(new SPOListFilter.TypeTemplateRange() { From = 101, To = 5000, });
            Assert.AreEqual(2, listFilter.Filter(testList).Count);
            listFilter.TitleFilters.Add(new SPOListFilter.TitleFilter() { Pattern = "Library 3", Include = true });
            Assert.AreEqual(3, listFilter.Filter(testList).Count);
            listFilter.TitleFilters.Clear();
            listFilter.TitleFilters.Add(new SPOListFilter.TitleFilter() { Pattern = "List", Include = true });
            Assert.AreEqual(4, listFilter.Filter(testList).Count);
            listFilter.TitleFilters.Add(new SPOListFilter.TitleFilter() { Pattern = "Library", Include = false });
            Assert.AreEqual(2, listFilter.Filter(testList).Count);
        }
        #endregion

        #region Get Fields
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t04_GetFields()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active) t04_GetFields1(ts);
        }
        private void t04_GetFields1(SPOTestSystem ts)
        {
             ISPOClient spoClient = createClient(ts);
            spoClient.Login();

            // Ensure there is a minimum number of field returned for our test library
            SPOList testLibrary = ((SPOClient)spoClient).GetListByTitle(ts.TestLibrary);
            List<SPOField> result = spoClient.GetFields(testLibrary);
            Assert.IsTrue(ts.FieldMin <= result.Count);
            Assert.AreEqual(1, result.Where(n => n.Title == "Title").Count());

            // Ensure the forcedField parameter worlds; "Title" should not matter.
            int cnt = result.Count;
            result = spoClient.GetFields(testLibrary, new List<string>() { "Name", "Title" });
            Assert.AreEqual(cnt + 1, result.Count);
        }
        #endregion

        #region Add Document and one field value
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t05_AddDocument()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active)
                {
                    t05_AddDocument1(ts, false, unitTestList);    // Lists
                    t05_AddDocument1(ts, true, unitTestLibrary);  // Libraries
                }
        }
        private void t05_AddDocument1(SPOTestSystem ts, bool isLibrary, string listName)
        {
            SPOClient spoc = createClient(ts) as SPOClient;
            spoc.Login();

            // Create list or library with one column
            SPOList spol = createListOrLibrary(spoc, isLibrary, listName,
                new List<SPOClient.FieldSpec>() {
                    new SPOClient.FieldSpec() { Name = "mySingleTextLine", Type = "Text" } });

            // Test without and with subfodlers. 
            // Always twice to handle the case where stuff already exists.
            addDocument(spoc, spol, "/MyFirstDocument.pdf");
            addDocument(spoc, spol, "MyFirstDocument.pdf");
            addDocument(spoc, spol, "sub/subsub/MyFirstDocument.pdf");
            addDocument(spoc, spol, "/sub/subsub/MySecondDocument.pdf");

            deleteListOrLibrary(spoc, listName);
        }

        private void addDocument(SPOClient spoc, SPOList spol, string documentPath)
        {
            // fields should contain just the one custom column..
            List<SPOField> fields = spoc.GetFields(spol).Where(n => n.Title != "Title").ToList();

            // Set a field value and upload document
            foreach (SPOField f in fields) f.Value = "Hello World!";
            int itemId = spoc.AddDocument(spol, documentPath, testDocument, fields);

            // Verify that the attribute is set
            foreach (SPOField f in fields) f.Value = null;
            spoc.GetFieldValues(spol, itemId, fields);
            foreach (SPOField f in fields) Assert.AreEqual("Hello World!", f.Value );
            //spoc.DeleteItem(spol, itemId);
        }
        #endregion

        #region Test column types
        /// Test various data types. Foreach data type a column in a list is created.
        /// The column is set to a value via AddDocument (without a file) and the result
        /// is verified. Actually the test is quite simple as SharePoint is pretty consistent
        /// as to what its input and output is.
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t06_ColumnTypes()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active) t06_ColumnTypes1(ts);
        }
        private void t06_ColumnTypes1(SPOTestSystem ts)
        {
            SPOClient spoc = createClient(ts) as SPOClient;
            spoc.Login();

            SPOList spol = createListOrLibrary(spoc, false, unitTestList);

            var td = new[]
            {
                new { n = 00, type = "Text", value = "HelloWorld", culture = "en-US" },
                new { n = 10, type = "DateTime", value = "02/15/2017", culture = "en-US" },
                new { n = 11, type = "DateTime", value = "15.02.2017", culture = "de-DE" },
                new { n = 12, type = "DateTime", value = "", culture = "de-DE" },
                new { n = 20, type = "Note", value = "Line1\nLine2", culture = "en-US" },
                new { n = 30, type = "Choice", value = "Berlin", culture = "en-US" },
                new { n = 40, type = "Number", value = "12.34", culture = "en-US" },
                new { n = 41, type = "Number", value = "12,34", culture = "de-DE" },
                new { n = 42, type = "Number", value = "", culture = "de-DE" },
                new { n = 50, type = "Currency", value = "14.22", culture = "en-US" },
                new { n = 51, type = "Currency", value = "", culture = "en-US" },
                new { n = 60, type = "Boolean", value = "False", culture = "en-US" },
                //new { n = 7, type = "Lookup", value = "abc", },
                //new { n = 8, type = "User", value = @"0#.w|vmsp2013\administrator", },
            };
            int doOnly = -1;
            for (int i = 0; i != td.Length; i++)
            {
                if (doOnly > 0 && td[i].n != doOnly) continue;

                string name = "a" + td[i].type;
                string type = td[i].type;
                spoc.AddFieldToList(spol, new SPOClient.FieldSpec() { Name = name, Type = type });
                spoc.ClientCulture = new CultureInfo(td[i].culture);
                testOneType(spoc, spol,type, name, td[i].value);
                spoc.DeleteField(spol, name);
            }
            Assert.IsTrue(doOnly < 0, "Not all tests executed");

            deleteListOrLibrary(spoc, unitTestList);
        }

        private void testOneType(SPOClient spoc, SPOList spoList, string type, string name, string value)
        {
            SPOField field = spoc.GetFields(spoList).Where(n => n.Title == name).First();
            field.Value = value;
            List<SPOField> fields = new List<SPOField>() { field };
            int itemId = spoc.AddDocument(spoList, null, null, fields);
            field.Value = null;
            spoc.GetFieldValues(spoList, itemId, fields);

            if (value == string.Empty)
            {
                Assert.IsNull(fields[0].Value);
                return;
            }
            // My theory on how things work:
            // Client -> Server: csom takes string and gives it to SharePoint. It is parsed by SharePoints culture
            // Server -> Client: csom receives data and converts it to string according to current culture
            Assert.AreEqual(
                normalize(spoc.ClientCulture, type, value), 
                normalize(CultureInfo.CurrentCulture, type, fields[0].Value));
        }

        private string normalize(CultureInfo culture, string type, string value)
        {
            switch (type)
            {
                case "DateTime":
                    return DateTime.Parse(value, culture).ToString();
                case "Number":
                case "Currency":
                    return float.Parse(value, culture).ToString();
                case "Text":
                case "Note":
                case "Choice":
                case "Boolean":
                case "Lookup":
                case "User":
                    return value;
                default: throw new Exception("Unknown type: " + type);
            }
        }
        #endregion

        #region Test Calculate missing folders
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t07_CalculateMissingFolders()
        {
            SPOClient spoClient = createClient(testsystems.First()) as SPOClient; // pick arbitrarily
            PrivateObject po = new PrivateObject(spoClient);
            po.SetField("serverUrl", "http://Server/mySite/");
            po.SetField("serverAuthority", "http://Server");
            po.SetField("serverPath", "/mySite/");

            List<string> existingFolders = new List<string>()
            {
                "/mySite/Lists/listTitle/a/b/c/d",
                "/mySite/Lists/listTitle/a/b/c",
                "/mySite/Lists/listTitle/a/b",
                "/mySite/Lists/listTitle/a",
                "/mySite/Lists/listTitle/xyz", 
            };
            string filename = "a/b/c/Document.pdf";
            List<SPOClient.FolderCreationSpec> result;
            string basepath = "http://Server/mySite/Lists/listTitle";
            string serverRelativeUrl = "/mySite/Lists/listTitle";

            var td = new[]
            {
                new { n = 0, result = new List<SPOClient.FolderCreationSpec>() { }, },
                new { n = 1, result = new List<SPOClient.FolderCreationSpec>() { }, },
                new { n = 2, result = new List<SPOClient.FolderCreationSpec>() {
                    new SPOClient.FolderCreationSpec()
                        { Path = basepath + "/a/b", Foldername = "c", }, },
                },
                new { n = 3, result = new List<SPOClient.FolderCreationSpec>() {
                    new SPOClient.FolderCreationSpec()
                        { Path = basepath + "/a", Foldername = "b", },
                    new SPOClient.FolderCreationSpec()
                        { Path = basepath + "/a/b", Foldername = "c", }, },
                },
                new { n = 4, result = new List<SPOClient.FolderCreationSpec>() {
                    new SPOClient.FolderCreationSpec()
                        { Path = basepath, Foldername = "a", },
                    new SPOClient.FolderCreationSpec()
                        { Path = basepath + "/a", Foldername = "b", },
                    new SPOClient.FolderCreationSpec()
                        { Path = basepath + "/a/b", Foldername = "c", }, },
                },
            };
            int doOnly = -1;
            for (int i = 0; i != td.Length; i++)
            {
                if (doOnly != -1 && td[i].n != doOnly)
                    continue;

                List<string> ef = existingFolders.Skip(td[i].n).ToList();
                result = (List<SPOClient.FolderCreationSpec>)
                    po.Invoke("calculateFolderCreationSequence",
                        new object[3] { serverRelativeUrl, ef, filename });

                Assert.AreEqual(td[i].result.Count, result.Count);
                for (int j = 0; j < td[i].result.Count; j++)
                {
                    Assert.AreEqual(td[i].result[j].Path, result[j].Path);
                    Assert.AreEqual(td[i].result[j].Foldername, result[j].Foldername);
                }
            }
            foreach (string fn in new List<string>() { "Document.pdf", "/Dokument.pdf" })
            {
                result = (List<SPOClient.FolderCreationSpec>)
                    po.Invoke("calculateFolderCreationSequence",
                        new object[3] {"listTitle", existingFolders, fn });
                Assert.AreEqual(0, result.Count());
            }
            Assert.AreEqual(-1, doOnly, "Not all tests executed");
        }
        #endregion

        #region Test Create and delete a list
        /// Kind of dummy test to fix errors from pre-exising lists
        /// http://stackoverflow.com/questions/42146301/deleting-list-does-not-work-list-does-not-exist
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t08_ListCreation()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active) t08_ListCreation1(ts);
        }
        private void t08_ListCreation1(SPOTestSystem ts)
        {
            SPOClient spoc = createClient(ts) as SPOClient;
            spoc.Login();

            string listname = unitTestList;
            //string listname = "abc";

            try { spoc.DeleteList(listname); } catch (Exception e) { var x = e.Message; }
            spoc.CreateList(listname, false);

            spoc = createClient(ts) as SPOClient;
            spoc.Login();

            spoc.DeleteList(listname);
        }
        #endregion

        #region Test list item count per folder
        [TestMethod]
        [TestCategory("SharePoint client test")]
        public void t08_ListItemCount()
        {
            foreach (SPOTestSystem ts in testsystems)
                if (ts.Active)
                {
                    SPOClient spoc = createClient(ts) as SPOClient;
                    spoc.Login();
                    t08_ListItemCount1(spoc, false, unitTestList);
                    t08_ListItemCount1(spoc, true, unitTestLibrary);
                }
        }
        private void t08_ListItemCount1(SPOClient spoc, bool isLibrary, string listname)
        {
            try { spoc.DeleteList(listname); } catch { };

            // Create list or library with one column
            SPOList spol = createListOrLibrary(spoc, isLibrary, listname,
                new List<SPOClient.FieldSpec>() {
                    new SPOClient.FieldSpec() { Name = "mySingleTextLine", Type = "Text" } });

            string foldername = "a/b/";

            var td = new[]
            {
                new { withFolder = true, root = 1, folder = 1 },
                new { withFolder = false, root = 2, folder = 1 },
                new { withFolder = false, root = 3, folder = 1 },
                new { withFolder = true, root = 3, folder = 2 },
            };

            verifyFolderContent(spoc, spol, "", 0);
            verifyFolderContent(spoc, spol, foldername, -1);

            for (int i = 0; i != td.Length; i++)
            {
                addDocument(spoc, spol, (td[i].withFolder ? foldername : string.Empty) + "Document_" + i + ".pdf");
                verifyFolderContent(spoc, spol, "", td[i].root);
                verifyFolderContent(spoc, spol, foldername, td[i].folder);
            }
            deleteListOrLibrary(spoc, listname);
        }

        public void verifyFolderContent(SPOClient spoc, SPOList spol, string foldername, int folderCnt)
        {
            Assert.AreEqual(folderCnt, spoc.GetFolderCount(spol, foldername));
        }
        #endregion

        #region Utilities
        private ISPOClient createClient(SPOTestSystem ts)
        {
            SPOClient spoClient = new SPOClient();
            spoClient.Office365 = ts.Office365;
            spoClient.SiteUrl = ts.SiteUrl;
            spoClient.Username = ts.Username;
            spoClient.SetPassowrd(ts.Password);
            spoClient.ClientCulture = new CultureInfo("en-US");
            return spoClient;
        }

        private SPOList createListOrLibrary(
            SPOClient spoc, 
            bool isLibrary, 
            string name, List<SPOClient.FieldSpec> fieldSpecs = null)
        {
            try { spoc.DeleteList(name); } catch (Exception e) { var x = e.Message; }
            return spoc.CreateList(name, isLibrary, fieldSpecs);
        }

        private void deleteListOrLibrary(SPOClient spoc, string name)
        {
            try { spoc.DeleteList(name); } catch { }
        }
        #endregion
    }
}
