using AssistantApp.Models;
using Microsoft.ML;
using System.IO;

namespace AssistantApp.Services
{
    public class MLService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private const string ModelFile = "DiagnosisModel.zip";
        private static readonly string CsvFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "training_data.csv");

        public MLService()
        {
            _mlContext = new MLContext(seed: 0);
        }

        public async Task TrainModelAsync()
        {
            var dataView = _mlContext.Data.LoadFromTextFile<ModelInput>(
                path: CsvFile,
                hasHeader: true,
                separatorChar: ',');

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                                outputColumnName: "LabelKey",
                                inputColumnName: nameof(ModelInput.Label))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                                labelColumnName: "LabelKey",
                                featureColumnName: nameof(ModelInput.Features)))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                                outputColumnName: "PredictedLabel",
                                inputColumnName: "PredictedLabel"));

            _model = pipeline.Fit(dataView);

            using var fs = new FileStream(ModelFile, FileMode.Create, FileAccess.Write, FileShare.Write);
            _mlContext.Model.Save(_model, dataView.Schema, fs);
        }

        public bool LoadModel()
        {
            if (!File.Exists(ModelFile))
                return false;

            DataViewSchema schema;
            using var fs = new FileStream(ModelFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            _model = _mlContext.Model.Load(fs, out schema);
            return true;
        }

        public string PredictDiagnosis(float[] featureVector)
        {
            if (_model == null && !LoadModel())
                return null;

            var engine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
            var input = new ModelInput { Features = featureVector };
            var output = engine.Predict(input);
            return output.PredictedLabel;
        }
    }
}
