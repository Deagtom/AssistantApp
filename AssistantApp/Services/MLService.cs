using AssistantApp.Data;
using AssistantApp.Models;
using Microsoft.ML;
using System.IO;

namespace AssistantApp.Services
{
    public class MLService
    {
        private readonly DatabaseService _dbService;
        private readonly MLContext _mlContext;
        private Dictionary<int, int> _symptomIndexMap;
        private Dictionary<int, string> _diagnosisNameMap;
        private ITransformer _trainedModel;
        private const string ModelFileName = "DiagnosisModel.zip";

        public MLService(DatabaseService dbService)
        {
            _dbService = dbService;
            _mlContext = new MLContext(seed: 0);
        }

        public async Task InitializeMappingsAsync()
        {
            var symptoms = await _dbService.GetAllSymptomsAsync();
            _symptomIndexMap = symptoms.Select((s, i) => new { s.Id, Index = i })
                                       .ToDictionary(pair => pair.Id, pair => pair.Index);
            var diagnoses = await _dbService.GetAllDiagnosesAsync();
            _diagnosisNameMap = diagnoses.ToDictionary(d => d.Id, d => d.Name);
        }

        public async Task TrainModelAsync()
        {
            await InitializeMappingsAsync();
            var records = await _dbService.GetUsageRecordsAsync();
            var inputs = new List<ModelInput>();
            foreach (var record in records.Where(r => r.DiagnosisId.HasValue))
            {
                var symptomList = await _dbService.GetSymptomsByUsageIdAsync(record.Id);
                var featureVector = new float[_symptomIndexMap.Count];
                foreach (var symptom in symptomList)
                {
                    if (_symptomIndexMap.TryGetValue(symptom.Id, out var idx))
                        featureVector[idx] = 1f;
                }
                var label = _diagnosisNameMap[record.DiagnosisId.Value];
                inputs.Add(new ModelInput { Features = featureVector, Label = label });
            }
            var data = _mlContext.Data.LoadFromEnumerable(inputs);
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                           .Append(_mlContext.Transforms.Concatenate("Features", nameof(ModelInput.Features)))
                           .Append(_mlContext.MulticlassClassification.Trainers
                               .SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features"))
                           .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            _trainedModel = pipeline.Fit(data);
            using var fs = new FileStream(ModelFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            _mlContext.Model.Save(_trainedModel, data.Schema, fs);
        }

        public bool LoadModel()
        {
            if (!File.Exists(ModelFileName))
                return false;
            DataViewSchema schema;
            using var fs = new FileStream(ModelFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            _trainedModel = _mlContext.Model.Load(fs, out schema);
            return true;
        }

        public async Task<string> PredictDiagnosisAsync(List<int> symptomIds)
        {
            if (_trainedModel == null && !LoadModel())
                return null;
            if (_symptomIndexMap == null)
                await InitializeMappingsAsync();
            var featureVector = new float[_symptomIndexMap.Count];
            foreach (var id in symptomIds)
            {
                if (_symptomIndexMap.TryGetValue(id, out var idx))
                    featureVector[idx] = 1f;
            }
            var engine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_trainedModel);
            var input = new ModelInput { Features = featureVector };
            var output = engine.Predict(input);
            return output.PredictedLabel;
        }
    }
}
