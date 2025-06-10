using AssistantApp.Models;
using Microsoft.ML;
using System;
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

        private bool LoadModel()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModelFile)))
                return false;
            _model = _mlContext.Model.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModelFile), out _);
            return true;
        }

        public async Task TrainModelAsync()
        {
            var data = _mlContext.Data.LoadFromTextFile<ModelInput>(CsvFile, hasHeader: true, separatorChar: ',');
            var pipeline = _mlContext.Transforms.Concatenate("Features", nameof(ModelInput.Features))
                           .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ModelInput.Label)))
                           .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                           .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            var model = pipeline.Fit(data);
            using (var fs = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModelFile)))
                _mlContext.Model.Save(model, data.Schema, fs);
            _model = model;
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

        public float[] PredictProbabilities(float[] featureVector)
        {
            if (_model == null && !LoadModel())
                return Array.Empty<float>();
            var engine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
            var input = new ModelInput { Features = featureVector };
            var output = engine.Predict(input);
            return output.Score;
        }
    }
}
