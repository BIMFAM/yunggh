using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace yunggh.Components
{
    public class InputData
    {
        // for loop
        [ColumnName("feature1")]
        public float input1;

        [ColumnName("feature2")]
        public float input2;
        // loop all columns

        // public float var 1
    }

    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId;

        [ColumnName("score")]
        public float[] Distances;
    }
}
