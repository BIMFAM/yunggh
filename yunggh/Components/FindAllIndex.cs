using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components
{
    public class FindAllIndex : GH_Component 
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public FindAllIndex()
          : base("FindAllIndex", "FindAllIndex",
              "find indexes",
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
            pManager.AddNumberParameter("list", "n", "input your list", GH_ParamAccess.list);
            pManager.AddNumberParameter("item", "i", "items to search all indexes", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("indexex", "in", "retrieved indexes in difference branches", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> numList = new List<double>();         
            double item = double.NaN;
            ///List<int> FindAllIndexOf(int i, List<int> num)
            if(!DA.GetDataList(0, numList)) { return; }
            if(!DA.GetData(1, ref item)) { return; }
            
            if (numList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No data input");
                return;
            }

            List<int> indexes = new List<int>();
            /*for (int idx = 0; idx < numList.Count; idx++)
            {
                idx = numList.IndexOf(item, idx);//idx is the starting index of the search
                if (idx == -1)// not found, index = -1
                    return;
                indexes.Add(idx);
            }*/

            DA.SetDataList(0, FindIndexes(item, numList));
        }

        List<int> FindIndexes(double item, List<double> numList)
        {
            List<int> indexes = new List<int>();
            for (int idx = 0; idx < numList.Count; idx++)
            {
                idx = numList.IndexOf(item, idx);//idx is the starting index of the search
                if (idx == -1)// not found, index = -1
                    return indexes;
                indexes.Add(idx);
            }

            return indexes;
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
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

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9CCAD877-11BE-47BD-901C-A69589C766EA"); }
        }
    }
}