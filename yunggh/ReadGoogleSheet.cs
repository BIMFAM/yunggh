using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class ReadGoogleSheet : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadGoogleSheet()
          : base("Read Google Sheet", "Read Google Sheet",
              "Read Google Sheet",
              "yung gh", "Data")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddTextParameter("Auth", "A", "Authentication location", GH_ParamAccess.item);
            pManager.AddTextParameter("Spreadsheet ID", "S", "Google Spreadsheet ID", GH_ParamAccess.item);
            pManager.AddTextParameter("Tab", "T", "Tab Name", GH_ParamAccess.item);

            // If you want to change properties of certain parameters,
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddBooleanParameter("Read", "R", "Boolean indicating if successfully read.", GH_ParamAccess.item);
            pManager.AddTextParameter("Data", "D", "Spreadsheet data formatted to tree", GH_ParamAccess.tree);

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.
            string authentication = "";
            string spreadsheet = "";
            string tab = "";

            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref authentication)) return;
            if (!DA.GetData(1, ref spreadsheet)) return;
            if (!DA.GetData(2, ref tab)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            if (!File.Exists(authentication))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Authenitcation Failed");
                return;
            }
            if (spreadsheet == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Spreadsheet Parameter Empty");
                return;
            }
            if (tab == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tab Parameter Empty");
                return;
            }

            // We're set to create the spiral now. To keep the size of the SolveInstance() method small,
            // The actual functionality will be in a different method:
            GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> data = ReadGoogleSpreadsheet(authentication, spreadsheet, tab);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, true);
            DA.SetDataTree(1, data);
        }

        // !!!NOTE: IF MODIFYING SCOPES!!!,
        // delete your previously saved credentials at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        private static string[] Scopes = { SheetsService.Scope.Spreadsheets };

        private static string ApplicationName = "Yung GH";

        private GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> ReadGoogleSpreadsheet(string authentication, String spreadsheetId, string tab)
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
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No data found in spreadsheet.");
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
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to septenary),
        /// each of which can be combined with the GH_Exposure.obscure flag, which
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.ReadGooglesheet;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd556"); }
        }
    }
}