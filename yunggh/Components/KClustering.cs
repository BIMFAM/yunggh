using System;
using System.Collections.Generic;
using Microsoft.ML;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System.Drawing;

namespace yunggh.Components
{
    public class KClustering : GH_Component
    {

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public KClustering()
          : base("KClusteringGroupOjects", "KGO",
              "cluster objects via KMeans",
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
            pManager.AddTextParameter("dataPath", "D", "data path of text file", GH_ParamAccess.item);          
            pManager.AddNumberParameter("nCluster", "N", "number of clusters", GH_ParamAccess.item);
            // TODO label boolean input
            pManager.AddBooleanParameter("Use custom color", "B", "whether to use custimized color", GH_ParamAccess.item);
            pManager.AddColourParameter("Color range", "C", "colors of clusters", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("categories", "n", "classified labels", GH_ParamAccess.list);
            pManager.AddColourParameter("clusterColor", "C", "colors of clusters", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string dataPath = "";
            bool UseCustomColor = false;
            double numberCluster = 2;
            List<Color> colors = new List<Color>();

            if (!DA.GetData(0, ref dataPath)) { return; }
            if (!DA.GetData(1, ref numberCluster)) { return; }
            DA.GetData(2, ref UseCustomColor);
            DA.GetDataList(3, colors);

            // There was cast error: fail from Number to int
            // trainner options require int
            // convert double to int
            int nCluster = Convert.ToInt32(numberCluster);

            // see output class: clusterPrediction
            List<uint> PredictedGroups = new List<uint>();
            foreach (var p in KMeansTrain(dataPath, nCluster))
            {
                PredictedGroups.Add(p.PredictedClusterId);
            }
            // use customized color
            List<Color> outputColors = new List<Color>();
            if (UseCustomColor)
            {
                for (int i = 0; i < PredictedGroups.Count; i++)

                    for (int j = 1; j < nCluster + 1; j++)
                    {
                        if (j.ToString() == PredictedGroups[i].ToString())
                        { outputColors.Add(colors[j]); }
                    }
            }
            // output
            DA.SetDataList(0, PredictedGroups);
            DA.SetDataList(1, outputColors);

        }

        private List<ClusterPrediction> KMeansTrain(string dataPath, int nCluster)
        {
            // ml environment
            // TODO? control seed?
            var mlContext = new MLContext(seed: 1);
            // load data from file path
            IDataView trainingData = mlContext.Data.LoadFromTextFile<InputData>(dataPath, separatorChar: ',', hasHeader: false);
            // normalization
            
            //define options
            // the innitialization algorithm is changed
            var options = new KMeansTrainer.Options
            {
                FeatureColumnName = "features",
                NumberOfClusters = nCluster,
                InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus
            };
            // pipeline
            var pipeline = mlContext.Transforms.Concatenate("features", "feature1", "feature2", "feature3")
                .Append(mlContext.Transforms.NormalizeMeanVariance("features"))
                .Append(mlContext.Clustering.Trainers.KMeans(options));

            // train the model
            var model = pipeline.Fit(trainingData);
            // do predictions using inputdata again
            // TODO it can add new test data
            var transformedTestData = model.Transform(trainingData);

            // prediction: create enumerable
            var predictions = mlContext.Data.CreateEnumerable<ClusterPrediction>(transformedTestData, reuseRowObject: false).ToList();

            return predictions;
        }

        

        /*private static IEnumerable<InputData> GetInputData()
        {

            InputData[] inputDatas = new InputData[]
            {
                new InputData
                {
                    input1 = 0.2f,
                    input2 = 0.4f,
                    input3 = 0.4f
                },
                new InputData
                {
                    input1 = 0.1f,
                    input2 = 0.2f,
                    input3 = 0.4f
                },
                new InputData
                {
                    input1 = 0.8f,
                    input2 = 0.9f,
                    input3 = 0.3f
                },
                new InputData
                {
                    input1 = 0.9f,
                    input2 = 0.7f,
                    input3 = 0.2f
                }
            };
            return inputDatas;
        }*/

       
        public class InputData
        {
            
            [LoadColumn(0)]
            [ColumnName("feature1")]
            public float input1;

            [LoadColumn(1)]
            [ColumnName("feature2")]
            public float input2;

            [LoadColumn(2)]
            [ColumnName("feature3")]
            public float input3;
            // public float var 1
        }

        public class ClusterPrediction
        {
            [ColumnName("PredictedLabel")]
            public uint PredictedClusterId;

            [ColumnName("Score")]
            public float[] Distances;
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        /// 

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
            get { return new Guid("829F2EEA-C9C5-40DD-A83E-29CFE92A37BC"); }
        }
    }
}