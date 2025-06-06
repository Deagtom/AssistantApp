using Microsoft.ML.Data;

public class ModelInput
{
    [VectorType]
    public float[] Features { get; set; }
    public string Label { get; set; }
}