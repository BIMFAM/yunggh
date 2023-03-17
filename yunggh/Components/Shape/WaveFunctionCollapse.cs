using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace yunggh
{
    public class WaveFunctionCollapse : GH_Component
    {
        public WaveFunctionCollapse()
          : base("Wave Function Collapse", "WaveFunctionCollapse",
              "Wave function collapse to build grids",
              "yung gh", "Shape")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh of pictures", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Rules", "R", "Rules for grids", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("X", "X", "X of grids", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Y", "Y", "Y of grids", GH_ParamAccess.item);
            pManager.AddIntegerParameter("X length", "XL", "X of grids", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Y length", "YL", "Y of grids", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Seed", "S", "Random seeds", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Times", "T", "Times of observe", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshs", "M", "Output meshs", GH_ParamAccess.list);
            pManager.AddCurveParameter("Grids", "G", "Girds", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> output = new List<Mesh>(); // output
            List<List<int>> notConnects = new List<List<int>>();

            List<Mesh> geo = new List<Mesh>();
            GH_Structure<GH_Integer> notConnectRules = new GH_Structure<GH_Integer>();
            Int32 x = 10;
            Int32 y = 10;
            Int32 dx = 10;
            Int32 dy = 10;
            Int32 seed = 0;
            

            if (!DA.GetDataList(0, geo) || !DA.GetDataTree(1, out notConnectRules)) return;
            DA.GetData(2, ref x);
            DA.GetData(3, ref y);
            DA.GetData(4, ref dx);
            DA.GetData(5, ref dx);
            DA.GetData(6, ref seed);

            Int32 times = x * y;
            DA.GetData(7, ref times);


            foreach (List<GH_Integer> not in notConnectRules.Branches)
            {
                List<Int32> nums = new List<Int32>();
                Int32 num = 0;
                foreach (GH_Integer n in not) {
                    n.CastTo(ref num);
                    nums.Add(num);
                }
                notConnects.Add(nums);
            }
            List<HashSet<int>> rules = createRules(geo, notConnects); // create rules
            List<Int32>[,] possibilityMap = buildMap(geo.Count, x, y);  // build map for grids

            Random rd = new Random(seed); //random generator

            for (int i = 0; i < times; i++)
            {
                int index = getMinGrid(rd, x, y, possibilityMap); //select one random gird
                if (index < 0) continue;
                observation(rd, index / y, index % y, x, y, dx, dy, ref possibilityMap, rules, geo, ref output); // observe
            }

            DA.SetDataList(0, output);
            DA.SetDataList(1, createGrids(x, y, dx, dy));
        }
        void observation(Random rd, int x, int y, int mx, int my, int dx, int dy, ref List<Int32>[,] map, List<HashSet<Int32>> rules, List<Mesh> geo, ref List<Mesh> output)
        { // select a random grid and set it
            if (x < 0 || x >= mx || y < 0 || y >= my) return;
            List<Int32> possibility = map[x, y];  // get possiblility of observed grid
            int type = possibility[rd.Next(0, possibility.Count)];  // get random type

            setGrid(x, y, dx, dy, type, geo, ref output); // set that grid
            possibility.Clear();  // claar possibility
                                  //map[x, y] = possibility;
            propagation(type, x + 1, y, mx, my, ref map, rules, geo, ref output);
            propagation(type, x - 1, y, mx, my, ref map, rules, geo, ref output);
            propagation(type, x, y + 1, mx, my, ref map, rules, geo, ref output);
            propagation(type, x, y - 1, mx, my, ref map, rules, geo, ref output);
        }

        void propagation(int type, int x, int y, int mx, int my, ref List<Int32>[,] map, List<HashSet<Int32>> rules, List<Mesh> geo, ref List<Mesh> output)
        {
            if (x < 0 || x >= mx || y < 0 || y >= my) return;
            List<Int32> possibility = map[x, y];  // get possibility
            HashSet<Int32> rule = rules[type];  // get rule
            for (int i = possibility.Count - 1; i >= 0; i--)
            {
                if (rule.Contains(possibility[i]))
                {
                    possibility.RemoveAt(i);
                }
            }
            //map[x, y] = possibility;
        }

        int getMinGrid(Random rd, int mx, int my, List<Int32>[,] map)
        {
            List<Int32> ret = new List<Int32>();
            int min = 100000;
            int mIndex = -1;
            for (int i = 0; i < mx; i++)
            {
                for (int j = 0; j < my; j++)
                {
                    if (map[i, j].Count < min && map[i, j].Count != 0)
                    {
                        min = map[i, j].Count;
                        mIndex = i * my + j;
                    }
                }
            }

            for (int i = 0; i < mx; i++)
            {  // get grids with min possibility
                for (int j = 0; j < my; j++)
                {
                    if (map[i, j].Count == min)
                    {
                        ret.Add(i * my + j);
                    }
                }
            }
            if (ret.Count == 0) return -1;
            mIndex = ret[rd.Next(0, ret.Count)];

            return mIndex;
        }

        List<Int32>[,] buildMap(int size, int x, int y)
        { //  build a posibility map for grids
            List<Int32>[,] map = new List<Int32>[x, y];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    List<Int32> possibility = new List<Int32>();
                    for (int k = 0; k < size; k++)
                    {  // add all possible choice into the set
                        possibility.Add(k);
                    }
                    map[i, j] = possibility;
                }
            }
            return map;
        }

        List<Rectangle3d> createGrids(int x, int y, int dx, int dy)
        {  // create base grids
            List<Rectangle3d> ret = new List<Rectangle3d>();
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    Rectangle3d rect = new Rectangle3d(new Plane(new Point3d(i * dx, j * dy, 0), new Vector3d(0, 0, 1)), dx, dy);
                    ret.Add(rect);
                }
            }
            return ret;
        }

        void setGrid(int x, int y, int dx, int dy, int type, List<Mesh> geo, ref List<Mesh> output)
        {  // add new grid to output
            Mesh ms = new Mesh();
            ms.CopyFrom(geo[type]);
            Vector3d oToP = new Vector3d(geo[type].Vertices[0]); //Point3f
            Vector3d oToN = new Vector3d(x * dx, (y + 1) * dy, 0);
            ms.Translate(-oToP);
            ms.Translate(oToN);
            //Transform tf = Transform.Scale(new Plane(new Point3d(1, 1, 0), new Vector3d(0, 0, 1)), dx/10, dy/10, 1);
            ms.Transform(Transform.Scale(new Plane(new Point3d(1, 1, 0), new Vector3d(0, 0, 1)), dx / 10, dy / 10, 1));
            output.Add(ms);
        }

        List<HashSet<Int32>> createRules(List<Mesh> geo, List<List<int>> notConnects)
        { // create rules for selection
            List<HashSet<Int32>> rules = new List<HashSet<Int32>>();
            foreach (List<int> rawRule in notConnects)
            {
                HashSet<Int32> rule = new HashSet<Int32>();
                foreach (int num in rawRule)
                {
                    rule.Add(num);
                }
                rules.Add(rule);
            }
            return rules;
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.PolarConvexity;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7DA755BF-1C9F-4F44-B1E8-18766F0E3320"); }
        }
    }
}