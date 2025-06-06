using System.IO;
using System.Threading.Tasks;
using AssistantApp.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace AssistantApp.Services
{
    public class MLService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private const string ModelFile = "DiagnosisModel.zip";
        private const string CsvFile = "Data/training_data_large.csv";

        public MLService()
        {
            _mlContext = new MLContext(seed: 0);
        }

        public Task TrainModelAsync()
        {
            var data = _mlContext.Data.LoadFromTextFile<ModelInput>(
                path: CsvFile,
                hasHeader: true,
                separatorChar: ',');

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(nameof(ModelInput.Label))
                .Append(_mlContext.Transforms.NormalizeMinMax(nameof(ModelInput.Features)))
                .Append(_mlContext.MulticlassClassification.Trainers
                    .SdcaMaximumEntropy(labelColumnName: nameof(ModelInput.Label), featureColumnName: nameof(ModelInput.Features)))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _model = pipeline.Fit(data);

            using var fs = new FileStream(ModelFile, FileMode.Create, FileAccess.Write, FileShare.Write);
            _mlContext.Model.Save(_model, data.Schema, fs);

            return Task.CompletedTask;
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