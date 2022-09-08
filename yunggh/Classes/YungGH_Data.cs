using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Threading;
using System.Diagnostics;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using System.Collections.Specialized;

namespace yunggh
{
    internal partial class YungGH
    {
        /// <summary>
        /// writes an array of data to a filepath.
        /// </summary>
        /// <param name="filepath">Output filepath</param>
        /// <param name="data">Array of data to write to file.</param>
        /// <returns>A boolean indicating successful write</returns>
        public bool Write(string filepath, string[] data)
        {
            //string filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
            //string folder = System.IO.Path.GetDirectoryName(filepath);
            //filepath = folder + "\\" + filename + ".csv";

            //writefile
            System.IO.File.WriteAllText(filepath, string.Join(Environment.NewLine, data), System.Text.Encoding.UTF8);
            return true;
        }

        /// <summary>
        /// Imports CSV data into a Grasshopper data tree.
        /// </summary>
        /// <param name="filepath">CSV filepath</param>
        /// <param name="delimiter">delimiter for CSV (default ',')</param>
        /// <returns>A data tree from csv</returns>
        public GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> ImportCSV(string filepath, string delimiter)
        {
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>
                treeArray = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();

            string[] text = System.IO.File.ReadAllLines(filepath);

            for (int i = 0; i < text.Length; i++)
            {
                string[] parts = text[i].Split(delimiter[0]); //split row
                for (int j = 0; j < parts.Length; j++)
                {
                    Grasshopper.Kernel.Data.GH_Path ghpath = new Grasshopper.Kernel.Data.GH_Path(i);
                    Grasshopper.Kernel.Types.GH_String cell = new Grasshopper.Kernel.Types.GH_String(parts[j]);
                    treeArray.Append(cell, ghpath);
                }
            }
            return treeArray;
        }

        /// <summary>
        /// !!!NOTE: IF MODIFYING SCOPES!!!,
        /// delete your previously saved credentials at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        /// </summary>
        private static string[] Scopes = { SheetsService.Scope.Spreadsheets };

        /// <summary>
        /// Application Name
        /// </summary>
        private static string ApplicationName = "Yung GH";

        /// <summary>
        /// Turn google spreadsheet data into a Grasshopper data tree.
        /// </summary>
        /// <param name="authentication">Google authentication .json</param>
        /// <param name="spreadsheetId">Name of spreadsheet</param>
        /// <param name="tab">Name of spreadsheet tab</param>
        /// <returns>A data tree of spreadsheet information</returns>
        public GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> ReadGoogleSpreadsheet(string authentication, String spreadsheetId, string tab)
        {
            //credential
            UserCredential credential;
            using (var stream =
                new FileStream(authentication, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Debug.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String range = tab;// + "!A2:E";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            //Read the cells in a spreadsheet:
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values == null || values.Count == 0)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No data found in spreadsheet.");
                return null;
            }

            //convert data
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>
                treeArray = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            int i = 0;
            foreach (var row in values)
            {
                for (int j = 0; j < row.Count; j++)
                {
                    Grasshopper.Kernel.Data.GH_Path ghpath = new Grasshopper.Kernel.Data.GH_Path(i);
                    Grasshopper.Kernel.Types.GH_String cell = new Grasshopper.Kernel.Types.GH_String(row[j].ToString());
                    treeArray.Append(cell, ghpath);
                }
                i++;
            }
            return treeArray;
        }

        /// <summary>
        /// Write data to google spreadsheet.
        /// </summary>
        /// <param name="authentication">Google authentication .json</param>
        /// <param name="spreadsheetId">Name of spreadsheet</param>
        /// <param name="tab">Name of spreadsheet tab</param>
        /// <param name="data">Data to add to spreadsheet</param>
        /// <returns>True if successful, false if not</returns>
        public bool WriteGoogleSpreadsheet(string authentication, String spreadsheetId, string tab, GH_Structure<Grasshopper.Kernel.Types.GH_String> data)
        {
            //credential
            UserCredential credential;
            using (var stream =
                new FileStream(authentication, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None//,
                //new FileDataStore(credPath, true)
                ).Result;
                //Debug.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            //Convert Data
            IList<IList<object>> convertedData = new List<IList<object>>();
            foreach (IList<Grasshopper.Kernel.Types.GH_String> list in data.Branches)
            {
                IList<object> row = new List<object>();
                foreach (Grasshopper.Kernel.Types.GH_String text in list)
                {
                    row.Add(text.ToString());
                }
                convertedData.Add(row);
            }

            //make sure the document exists
            SpreadsheetsResource.GetRequest getRequest = service.Spreadsheets.Get(spreadsheetId);
            Spreadsheet resource = getRequest.Execute();

            //make sure the sheet exists, if not add it.
            bool sheetExists = false;
            foreach (Sheet sheet in resource.Sheets) { if (sheet.Properties.Title == tab) { sheetExists = true; } }
            if (!sheetExists)
            {
                AddNewTab(service, spreadsheetId, tab);
            }

            //Writing
            ValueRange Update = new ValueRange();
            Update.Values = convertedData;
            Update.Range = tab;

            //UPDATE
            SpreadsheetsResource.ValuesResource.UpdateRequest Updaterequest = service.Spreadsheets.Values.Update(Update, spreadsheetId, tab);
            Updaterequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            UpdateValuesResponse Traderesult = Updaterequest.Execute();
            return true;
        }

        /// <summary>
        /// Add new tab to google spreadsheet
        /// </summary>
        /// <param name="service">Scope</param>
        /// <param name="spreadsheetId">Spreadsheet ID</param>
        /// <param name="sheetName">Name of spreadsheet tab</param>
        private void AddNewTab(SheetsService service, string spreadsheetId, string sheetName)
        {
            var addSheetRequest = new AddSheetRequest();
            addSheetRequest.Properties = new SheetProperties();
            addSheetRequest.Properties.Title = sheetName;
            BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
            batchUpdateSpreadsheetRequest.Requests = new List<Request>();
            batchUpdateSpreadsheetRequest.Requests.Add(new Request
            {
                AddSheet = addSheetRequest
            });

            var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);

            batchUpdateRequest.Execute();
        }

        /// <summary>
        /// Read Rhino Object text attributes into dictionary.
        /// </summary>
        /// <param name="obj">Rhino Object</param>
        /// <returns>Attributes as  Dictionary</returns>
        public Dictionary<string, string> ReadObjectAttributes(Rhino.DocObjects.RhinoObject obj)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            Rhino.DocObjects.ObjectAttributes objattributes = new Rhino.DocObjects.ObjectAttributes();
            objattributes = obj.Attributes;

            NameValueCollection keyValues = objattributes.GetUserStrings();
            foreach (string key in keyValues)
            {
                attributes.Add(key, keyValues[key]);
            }
            return attributes;
        }

        /// <summary>
        /// Write Rhino Object attributes. If the object exists in the Rhino Document, attributes are added.
        /// </summary>
        /// <param name="obj">Rhino Object</param>
        /// <param name="name">Object name</param>
        /// <param name="key">List of Attribute keys</param>
        /// <param name="val">List of Attribute values</param>
        /// <param name="clean">If true, removes keys if not existing in key list. If false, keeps existing keys</param>
        /// <returns>Object Attributes (even if object not existing)</returns>
        public Rhino.DocObjects.ObjectAttributes WriteObjectAttributes(object obj, string name, List<string> key, List<string> val, bool clean)
        {
            Rhino.DocObjects.ObjectAttributes objattributes = new Rhino.DocObjects.ObjectAttributes();

            if (obj is System.Guid || obj is GH_Guid)
            {
                System.Guid guid = System.Guid.Empty;
                if (obj is System.Guid)
                    guid = (System.Guid)obj;
                else
                {
                    GH_Guid gh_guid = (GH_Guid)obj;
                    guid = gh_guid.Value;
                }
                Rhino.DocObjects.RhinoObject rhinoObject = Rhino.RhinoDoc.ActiveDoc.Objects.Find(guid);

                //get existing attributes if we are writing clean attributes (meaning we delete the old name and attributes)
                if (!clean)
                    objattributes = rhinoObject.Attributes;

                //add name if it isn't null or empty
                if (!String.IsNullOrEmpty(name) && name != "")
                    objattributes.Name = name;

                //add attributes
                for (int i = 0; i < key.Count; i++)
                {
                    objattributes.SetUserString(key[i], val[i]);
                }

                //update object in rhino
                rhinoObject.Attributes = objattributes;
                rhinoObject.CommitChanges();
            }
            else
            {
                //set object attributes
                objattributes.Name = name;
                for (int i = 0; i < key.Count; i++)
                {
                    objattributes.SetUserString(key[i], val[i]);
                }
            }

            return objattributes;
        }
    }
}