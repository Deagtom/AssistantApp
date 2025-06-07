using Microsoft.ML.Data;

namespace AssistantApp.Models
{
    public class ModelInput
    {
        [LoadColumn(0, 24)]
        [VectorType(25)]
        public float[] Features { get; set; }

        [LoadColumn(25)]
        [ColumnName("Label")]
        public string Label { get; set; }
    }
}