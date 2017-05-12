using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Security;

namespace CaptureCenter.SPO
{
    class SPOClientMock : ISPOClient
    {
        public string SiteUrl { get; set; }
        public string Username { get; set; }
        public SecureString Password { get; set; }
        public bool Office365 { get; set; }
        public CultureInfo ClientCulture { get; set; }

        public string GetServer() { return null; }
        public void SetPassowrd(string password) { }

        public void Login()
        {
            if (Username == "illegal") throw new Exception("Login error");
        }

        private List<SPOList> testList = new List<SPOList>()
        {
            new SPOList() { Id = Guid.NewGuid(), BaseType = 0, TemplateType = 100, Title = "List 1" },
            new SPOList() { Id = Guid.NewGuid(), BaseType = 1, TemplateType = 100, Title = "List 2" },
            new SPOList() { Id = Guid.NewGuid(), BaseType = 1, TemplateType = 101, Title = "Library 1" },
            new SPOList() { Id = Guid.NewGuid(), BaseType = 1, TemplateType = 5000, Title = "Library 2" },
            new SPOList() { Id = Guid.NewGuid(), BaseType = 1, TemplateType = 5005, Title = "Library 3" },
        };

        public bool ReturnEmptyList = false;
        public List<SPOList> GetLists() {
            if (ReturnEmptyList) return new List<SPOList>();
            return testList;
        }

        public List<SPOField> GetFields(SPOList spoList, List<string> forcedFields = null)
        {
            return new List<SPOField>()
            {
                new SPOField() { Title = "Title", DefaultValue = "use", InternalName="Id1", CustomField = false, TypeName = "Text" },
                new SPOField() { Title = "Name1", DefaultValue = "not", InternalName="Id2", CustomField = false, TypeName = "Text" },
                new SPOField() { Title = "Name2", DefaultValue = "use", InternalName="Id3", CustomField = true, TypeName = "Boolean" },
                new SPOField() { Title = "Name3", DefaultValue = "not", InternalName="Id4", CustomField = true, TypeName = "Unsupported" },
            };
        }

        public int MaxExistingFolderNumer { get; set; } = 0;
        public bool FolderExists(SPOList spol, string foldername)
        {
            int folderNumber = int.Parse(foldername.Substring(foldername.Length - 4, 4));
            return folderNumber <= MaxExistingFolderNumer;
        }

        public int GetFolderCount(SPOList list, string foldername)
        {
            if (folders.ContainsKey(foldername))
                return folders[foldername];
            return -1;
        }

        public class ExportResult
        {
            public string ListTitle { get; set; }
            public string DocumentPath { get; set; }
            public string Folder { get; set; }
            public string FilePath { get; set; }
            public List<SPOField> Fields { get; set; }
        }

        private Dictionary<string, int> folders = new Dictionary<string, int>();

        public ExportResult LastExportResult { get; set; }

        public int AddDocument(SPOList list, string documentPath, string filePath, List<SPOField> fields)
        {
            string folder = string.Join("/", documentPath.Split('/').Reverse().Skip(1).Reverse());
            if (!folders.ContainsKey(folder)) folders[folder] = 0;
            folders[folder]++;

            LastExportResult = new ExportResult()
            {
                ListTitle = list.Title,
                DocumentPath = documentPath,
                Folder = folder,
                FilePath = filePath,
                Fields = fields,
            };
            return fields.Count;
        }

        public bool IsTypeSupported(string type)
        {
            return new SPOClient().IsTypeSupported(type);
        }

        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
}
