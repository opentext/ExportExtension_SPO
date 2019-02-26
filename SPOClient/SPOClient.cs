using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Net;
using System.Globalization;
using Microsoft.SharePoint.Client;
using System.Xml.Linq;
using System.IO;

namespace CaptureCenter.SPO
{
    #region SPO client interface (including exchange data types)
    public interface ISPOClient : IDisposable
    {
        bool Office365 { get; set; }
        string SiteUrl { get; set; }
        string Username { get; set; }
        SecureString Password { get; set; }
        CultureInfo ClientCulture { get; set; }

        string GetServer();
        void SetPassowrd(string password);
        void Login();
        List<SPOList> GetLists();
        List<SPOField> GetFields(SPOList spoList, List<string> forcedFields = null);
        bool FolderExists(SPOList spol, string foldername);
        int GetFolderCount(SPOList spol, string foldername);
        int AddDocument(SPOList spol, string documentPath, string filePath, List<SPOField> fields);
        bool IsTypeSupported(string type);
    }

    [Serializable]
    public class SPOList
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int BaseType { get; set; }
        public int TemplateType { get; set; }

        public SPOList() { }

        public SPOList(List l)
        {
            Id = l.Id;
            Title = l.Title;
            BaseType = (int)l.BaseType;
            TemplateType = (int)l.BaseTemplate;
        }
    }

    [Serializable]
    public class SPOField
    {
        public string Title { get; set; }
        public string InternalName { get; set; }
        public string TypeName { get; set; }
        public object DefaultValue { get; set; }
        public bool CustomField { get; set; }
        public bool Use { get; set; }
        public string Value { get; set; }
    }
    #endregion

    /// The implementation has two parts. One part implements the interface, the other part
    /// provide functions for unit testing.
    public class SPOClient : ISPOClient
    {
        #region SPO client interface implementation

        private string siteUrl;
        public string SiteUrl
        {
            get { return siteUrl; }
            set {
                siteUrl = value;
                if (siteUrl[siteUrl.Length - 1] != '/') siteUrl += "/";
            }
        }
        public string Username { get; set; }
        public SecureString Password { get; set; }
        public bool Office365 { get; set; }
        public CultureInfo ClientCulture { get; set; }

        private ClientContext context;      // interface object for CSOM
        private string serverUrl;           // e.g. https://opentext.sharepoint.com/sites/occ/
        private string serverAuthority;     // e.g. https://opentext.sharepoint.com
        private string serverPath;          // e.g. sites/occ/
        CultureInfo serverCulture;

        public string GetServer() { return new Uri(SiteUrl).Host; }
        // In case we have a string password it needs to be converted to SecureString
        public void SetPassowrd(string password)
        {
            Password = new SecureString();
            foreach (char c in password.ToCharArray())
                Password.AppendChar(c);
        }

        // Use different credential types for regular SharePoint and SharePoint online
        public void Login()
        {
            try { context = new ClientContext(SiteUrl); }
            catch (Exception e)
            {
                throw new Exception("Cannot connect to " + SiteUrl + "\nReason: " + e.Message);
            }
            if (Office365)
            {
                SharePointOnlineCredentials cred = new SharePointOnlineCredentials(Username, Password);
                context.Credentials = cred;
            }
            else
            {
                NetworkCredential cred = new NetworkCredential(Username, Password);
                context.Credentials = cred;
            }
            // Verify that the credentials are ok and the site is reachable
            Web myWeb = context.Web;
            context.Load(myWeb, wde => wde.Title);
            context.ExecuteQuery();

            // Initialize server information
            serverUrl = context.Url;
            Uri uri = new Uri(serverUrl);
            serverAuthority = uri.GetLeftPart(UriPartial.Authority);
            serverPath = uri.PathAndQuery;

            // Get server culture
            var web = context.Web;
            context.Load(web.RegionalSettings);
            context.ExecuteQuery();
            var localId = (int)web.RegionalSettings.LocaleId;
            serverCulture = CultureInfo.GetCultureInfo(Convert.ToInt32(localId));
        }

        // Straight forward: Return the all list from this site
        public List<SPOList> GetLists()
        {
            List<SPOList> result = new List<SPOList>();
            context.Load(context.Web.Lists);
            context.ExecuteQuery();

            foreach (List l in context.Web.Lists)
                result.Add(new SPOList(l));
            return result;
        }

        /// Get all usable fields from a lists
        /// Basically all user defined fields are deliverd. There is the option to
        /// force specific fields to show up in the result.
        public List<SPOField> GetFields(SPOList spoList, List<string> forcedFields = null)
        {
            List<SPOField> result = new List<SPOField>();

            // Ensure that the field "Title" is always in the result
            if (forcedFields == null) forcedFields = new List<string>();
            if (!forcedFields.Contains("Title")) forcedFields.Add("Title");

            // Load all field definitions from SharePoint
            List spList = context.Web.Lists.GetById(spoList.Id);
            context.Load(spList);
            context.Load(spList.RootFolder);
            context.Load(spList.Fields);
            context.ExecuteQuery();

            // Select the field we need to deliver
            foreach (Field f in spList.Fields)
            {
                // Always ignore redonly fields and hidden fields
                // Hidden does not mean visibility in the view, by the way
                if (f.ReadOnlyField || f.Hidden) continue;

                // A custom defined field can be identified the SourceID attribute is a GUID
                XElement fl = XElement.Parse(f.SchemaXml);
                bool customField = (Guid.TryParse(fl.Attribute("SourceID").Value, out Guid xx));
                bool forcedField = forcedFields.Contains(f.Title);

                if ( !(forcedField || customField) ) continue;

                result.Add(new SPOField()
                {
                    Title = f.Title,
                    InternalName = f.InternalName,
                    TypeName = f.TypeAsString,
                    DefaultValue = f.DefaultValue,
                    CustomField = customField,
                });
            }
            return result;
        }

        public bool FolderExists(SPOList spol, string foldername)
        {
            return getFolder(spol, foldername) != null;
        }

        private Folder getFolder(SPOList spol, string foldername)
        {
            if (!string.IsNullOrEmpty(foldername) && foldername[foldername.Length - 1] == '/')
                foldername = foldername.Substring(0, foldername.Length - 1);

            List spList = context.Web.Lists.GetById(spol.Id);
            context.Load(spList);

            if (string.IsNullOrEmpty(foldername))
                return spList.RootFolder;
            else
                return navigateToFolder(spList, foldername);
        }

        public int GetFolderCount(SPOList spol, string foldername)
        {
            Folder f = getFolder(spol, foldername);

            if (f == null) return -1;

            context.Load(f);
            context.ExecuteQuery();
            return f.ItemCount;
        }

        private Folder navigateToFolder(List spList, string path)
        {
            context.Load(spList.RootFolder);
            context.ExecuteQuery();
            Folder f = context.Web.GetFolderByServerRelativeUrl(
                spList.RootFolder.ServerRelativeUrl + "/" + path);
            context.Load(f);
            try { context.ExecuteQuery(); } catch { return null; }
            return f;
        }

        // Add a document and set the attribute values.
        // There are different paths for libraries and lists 
        public int AddDocument(SPOList spol, string documentPath, string filePath, List<SPOField> fields)
        {
            if (!String.IsNullOrEmpty(documentPath) && documentPath[0] == '/')
                documentPath = documentPath.Substring(1, documentPath.Length - 1);

            if (isLibrary(spol))
                return addDocumenToLibrary(spol, documentPath, filePath, fields);
            else
                return addDocumentToList(spol, documentPath, filePath, fields);
        }

        private int addDocumenToLibrary(SPOList spol, string documentPath, string filePath, List<SPOField> fields)
        {
            // Get the list
            List spList = context.Web.Lists.GetById(spol.Id);
            context.Load(spList);

            string documentName = documentPath.Split('/').Last();
            string documentFolder = string.Join("/", documentPath.Split('/').Reverse().Skip(1).Reverse());

            Folder f = spList.RootFolder;
            if (!string.IsNullOrEmpty(documentFolder))
                // Add the folders one by one. If folders do not exist they will be created smoothly.
                foreach (string folder in documentFolder.Split('/'))
                    f = f.Folders.Add(folder);

            // Create the document in SharePoint
            FileCreationInformation fci = new FileCreationInformation()
            {
                Url = documentName,
                Overwrite = true,
                Content = System.IO.File.ReadAllBytes(filePath),
            };
            Microsoft.SharePoint.Client.File uploadedFile = f.Files.Add(fci);

            // Add the attribute values. Title defaults to the document name.
            if (fields == null) fields = new List<SPOField>();
            foreach (SPOField spoField in fields)
            {
                string value = convert(spoField);
                if (value!= null)
                    uploadedFile.ListItemAllFields[spoField.InternalName] = value;
            }

            if (fields.Where(n => n.Title == "Title").Count() == 0)
                uploadedFile.ListItemAllFields["Title"] = documentName;

            // Update and execute. Return the Id of the new item.
            uploadedFile.ListItemAllFields.Update();
            context.ExecuteQuery();
            return GetListItem(spol, documentPath);
        }

        // Lists are more complex to handle the libraries.
        // If the is not´document given we do not upload one. (Used for unit testing)
        private int addDocumentToList(SPOList spol, string documentPath, string filePath, List<SPOField> fields)
        {
            // Get the list
            List spList = context.Web.Lists.GetById(spol.Id);
            context.Load(spList);
            context.Load(spList.RootFolder);
            context.ExecuteQuery();
            string serverRelativeUrl = spList.RootFolder.ServerRelativeUrl;

            string documentName = null;
            string documentFolder = null;
            if (documentPath != null)
            {
                documentName = documentPath.Split('/').Last();
                documentFolder = string.Join("/", documentPath.Split('/').Reverse().Skip(1).Reverse());
            }
            ListItemCreationInformation lici = new ListItemCreationInformation();

            // Create subfolders as needed
            if (documentPath != null)
            {
                // Figure out which folders need to be created
                List<FolderCreationSpec> folderCreatíonSequence =
                    calculateFolderCreationSequence(serverRelativeUrl, getAllFolderNames(spList), documentPath);

                // Create those folders
                foreach (FolderCreationSpec fcs in folderCreatíonSequence)
                    createFolder(spList, fcs.Path, fcs.Foldername);

                // Set the FolderUrl for the item creation
                lici.FolderUrl = serverRelativeUrl;
                if (!string.IsNullOrEmpty(documentFolder)) lici.FolderUrl += "/" + documentFolder;
            }
            // Create the item
            ListItem newItem = spList.AddItem(lici);

            // Set attribute values. Default for "Title" is the document name.
            if (fields == null) fields = new List<SPOField>();
            foreach (SPOField spoField in fields)
            {
                string value = convert(spoField);
                if (value != null)
                    newItem[spoField.InternalName] = value;
            }

            if (fields.Where(n => n.Title == "Title").Count() == 0)
                newItem["Title"] = documentName;

            newItem.Update();

            // Attach the file
            if (documentPath != null)
            {
                AttachmentCreationInformation attachment = new AttachmentCreationInformation()
                {
                    FileName = documentName,
                    ContentStream = new MemoryStream(System.IO.File.ReadAllBytes(filePath))
                };
                Attachment att = newItem.AttachmentFiles.Add(attachment);
                context.Load(att);
            }

            // Execution...
            context.ExecuteQuery();
            return newItem.Id;
        }

        private string convert(SPOField spoField)
        {
            switch (spoField.TypeName)
            {
                case "DateTime":
                    if (string.IsNullOrEmpty(spoField.Value)) return null;
                    return DateTime.Parse(spoField.Value, ClientCulture).ToString(serverCulture);
                case "Number":
                case "Currency":
                    if (string.IsNullOrEmpty(spoField.Value)) return null;
                    return float.Parse(spoField.Value, ClientCulture).ToString(serverCulture);
                default: return spoField.Value;
            }
        }

        // Create a folder. The process is controlled by FolderUrl and LeafName. 
        // FolderUrl must not be set when the folder is created directly under the root.
        private void createFolder(List spList, string path, string foldername)
        {
            var folderCreateInfo = new ListItemCreationInformation()
            {
                FolderUrl = path,
                LeafName = foldername,
                UnderlyingObjectType = FileSystemObjectType.Folder,
            };
            ListItem folderItem = spList.AddItem(folderCreateInfo);
            folderItem["Title"] = foldername;
            folderItem.Update();
        }

        // These types have nee proven to be settable. Others may work too.
        private List<string> supportedTypes = new List<string>()
        {
            "DateTime", "Text", "Note", "Choice", "Number",
            "Currency", "Boolean", "Lookup", "User",
        };

        public bool IsTypeSupported(string type)
        {
            return supportedTypes.Contains(type);
        }

        // FolderCreationSpec specifies the name of a folder to be created 
        // and where it has to be created.
        public struct FolderCreationSpec
        {
            public string Path { get; set; }
            public string Foldername { get; set; }
        }

        // This is probably the most complicated algorithm in this module.
        // If the file path is a/b/xyz.pdf we check for "a/b" and "a" in the
        // list of existing folders and tell from there what is needed.
        // Most of the complexity stems from the fact that
        //  * the filePath is just the file path
        //  * the existingFolders have the siteName + "Lists" + list title in front
        //  * the result should contain a full URL of the folder 
        private List<FolderCreationSpec> calculateFolderCreationSequence(
            string serverRelativeUrl, List<string> existingFolders, string filePath)
        {
            List<FolderCreationSpec> result = new List<FolderCreationSpec>();

            // Ignore leadíng `"/" in the filePath
            if (filePath[0] == '/') filePath = filePath.Substring(1);

            // If only a filename is give there is nothing to do
            if (filePath.Split('/').Count() == 1) return result;

            // The initial match path that is shortened segment by segment
            string matchPath = serverRelativeUrl + "/" +
                string.Join("/", filePath.Split('/').Reverse().Skip(1).Reverse());
            string filename = filePath.Split('/').Last();

            while (matchPath != serverRelativeUrl)
            {
                // Check whether we're done
                if (existingFolders.Contains(matchPath)) break;

                // New folder is rightmost segment
                string newFolder = string.Join("/", matchPath.Split('/').Last());
                // New matchPath is all but the rightmost segement
                matchPath = string.Join("/", matchPath.Split('/').Reverse().Skip(1).Reverse());
                // Result is the complete Url
                result.Add(new FolderCreationSpec() { Path = serverAuthority + matchPath, Foldername = newFolder });
            }
            return result.Where(n => n.Foldername == n.Foldername).Reverse().ToList();
        }

        private List<string> getAllFolderNames(List spList)
        {
            CamlQuery camlQuery = new CamlQuery()
            {
                ViewXml =
                    "<View Scope='RecursiveAll'> <Query> <Where> <Eq>" +
                    "<FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value>" +
                    "</Eq> </Where> </Query> </View>"
            };
            ListItemCollection collListItem = spList.GetItems(camlQuery);
            context.Load(collListItem);
            context.ExecuteQuery();

            List<string> result = new List<string>();
            foreach (ListItem li in collListItem)
                result.Add(li.FieldValues.Where(n => n.Key == "FileRef").Select(n => n.Value).First() as string);

            return result;
        }

        private bool isLibrary(SPOList spol)
        {
            return spol.TemplateType == (int)ListTemplateType.DocumentLibrary;
        }
        #endregion

        #region Functions for unit tests
        public SPOList GetListByTitle(string title)
        {
            List spList = context.Web.Lists.GetByTitle(title);
            context.Load(spList);
            context.ExecuteQuery();
            return new SPOList(spList);
        }

        public int GetListItem(SPOList spoList, string documentPath)
        {
            List spList = context.Web.Lists.GetById(spoList.Id);
            context.Load(spList.RootFolder);
            context.ExecuteQuery();
            string path = spList.RootFolder.ServerRelativeUrl + "/" + documentPath;

            CamlQuery query = new CamlQuery()
            {
                ViewXml = "<View Scope='RecursiveAll'>"
               + "<Query>"
               + $"<Where><Eq><FieldRef Name='FileRef'/><Value Type='File'>{path}</Value></Eq></Where>"
               + "</Query>"
               + "</View>"
            };
            // execute the query                
            ListItemCollection listItems = spList.GetItems(query);
            context.Load(listItems);
            context.ExecuteQuery();
            return listItems.First().Id;
        }

        public void GetFieldValues(SPOList spoList, int itemId, List<SPOField> fields)
        {
            List spList = context.Web.Lists.GetById(spoList.Id);
            ListItem li = spList.GetItemById(itemId);
            context.Load(li);
            context.ExecuteQuery();
            foreach (SPOField f in fields)
            {
                object value = li.FieldValues[f.InternalName];
                if (value != null) f.Value = value.ToString();
            }
        }

        public void DeleteItem(SPOList spoList, int itemId)
        {
            List spList = context.Web.Lists.GetById(spoList.Id);
            ListItem li = spList.GetItemById(itemId);
            li.DeleteObject();
            context.ExecuteQuery();
        }

        public SPOList CreateList(string title, bool isLibrary, List<FieldSpec> fieldSpecs = null)
        {
            ListCreationInformation lci = new ListCreationInformation()
            {
                Title = title,
                TemplateType = isLibrary ? 
                    (int)ListTemplateType.DocumentLibrary : 
                    (int)ListTemplateType.GenericList
            };
            List spList = context.Web.Lists.Add(lci);
            context.Load(spList);
            spList.Update();
            context.ExecuteQuery();
            SPOList spol = new SPOList(spList);

            if (fieldSpecs != null) foreach (FieldSpec fs in fieldSpecs)
                    AddFieldToList(spol, fs);
            return spol;
        }

        public void DeleteList(string listname)
        {
            List spList = context.Web.Lists.GetByTitle(listname);
            spList.DeleteObject();
            context.ExecuteQuery();
        }

        public struct FieldSpec
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }

        public void AddFieldToList(SPOList spoList, FieldSpec fs)
        {
            List spList = context.Web.Lists.GetById(spoList.Id);
            string fieldSchema = 
                "<Field ID='" + Guid.NewGuid() + "' " +
                "Type='" + fs.Type +  "' " +
                "Name='" + fs.Name + "' " +
                "StaticName='" + fs.Name + "' " +
                "DisplayName='" + fs.Name + "' />";
            Field field = spList.Fields.AddFieldAsXml(fieldSchema, true, AddFieldOptions.AddToDefaultContentType);
            context.ExecuteQuery();
        }

        public void DeleteField(SPOList spoList, string name)
        {
            List spList = context.Web.Lists.GetById(spoList.Id);
            Field f = spList.Fields.GetByTitle(name);
            f.DeleteObject();
            context.ExecuteQuery();
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing) context.Dispose();
                _disposed = true;
            }
        }
        #endregion
    }
}
