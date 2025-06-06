using Microsoft.ML.Data;

namespace AssistantApp.Models
{
    public class ModelInput
    {
        [LoadColumn(0, 9)]
        [VectorType(10)]
        public float[] Features { get; set; }

        [LoadColumn(10)]
        public string Label { get; set; }
    }
}