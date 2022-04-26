using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components
{
    public class WriteInputData : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public WriteInputData()
          : base("WriteInputML", "InputMlData",
              "write ml data for ml",
              "yung gh", "JJJ")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Write", "W", "Write text file", GH_ParamAccess.item);
            //pManager.AddTextParameter("Filepath", "F", "Filepath for writing", GH_ParamAccess.item);
            pManager.AddTextParameter("Data1", "D", "List of strings to write to file", GH_ParamAccess.list);
            pManager.AddTextParameter("Data2", "D", "List of strings to write to file", GH_ParamAccess.list);
            pManager.AddTextParameter("Data3", "D", "List of strings to write to file", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("DataPath", "D", "data path for ml", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            //string filepath = "";
            List<string> data1 = new List<string>();
            List<string> data2 = new List<string>();
            List<string> data3 = new List<string>();

            
            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref run)) return;
            //if (!DA.GetData(1, ref filepath)) return;
            if (!DA.GetDataList(1, data1)) return;
            DA.GetDataList(2, data2);
            DA.GetDataList(3, data3);
            // safer way to create file path, this is also the output for ML model.
            string filepath = Path.Combine(Environment.CurrentDirectory, "test.txt");
            // nested list
            List<List<string>> allData = new List<List<string>>();

            // this is equal to flip matrix. 
            // TODO use 'flip matrix'
            // TODO add inputs using "IGH_VariableParameterComponent"
            for (int i = 0; i < data1.Count; i++)
            {
                List<string> tempList = new List<string>();

                {
                    tempList.Add(data1[i]);
                    tempList.Add(data2[i]);
                    tempList.Add(data3[i]);
                };
                allData.Add(tempList);
            }
            // write list to test file.
            using (var file = File.CreateText(filepath))
            foreach (var list in allData)
            {
                    file.WriteLine(string.Join(",", list));
            }
            // output test file path.
            DA.SetData(0, filepath);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DA8DC972-0469-40D3-A4AF-82FA09C1BAE5"); }
        }
    }
}