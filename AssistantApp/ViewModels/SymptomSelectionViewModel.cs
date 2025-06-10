using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Globalization;
using AssistantApp.Data;
using AssistantApp.Models;
using AssistantApp.Services;
using AssistantApp.Converters;

namespace AssistantApp.ViewModels
{
    public class DiagnosisProbability
    {
        public Diagnosis Diagnosis { get; }
        public string Name => Diagnosis.Name;
        public float Probability { get; }

        public DiagnosisProbability(Diagnosis diagnosis, float probability)
        {
            Diagnosis = diagnosis;
            Probability = probability;
        }
    }

    public class SymptomSelectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly MLService _mlService;
        private readonly LocalizationMultiConverter _locConverter = new LocalizationMultiConverter();

        private bool _isSymptomMode = true;
        public bool IsSymptomMode
        {
            get => _isSymptomMode;
            set
            {
                if (_isSymptomMode == value) return;
                _isSymptomMode = value;
                OnPropertyChanged(nameof(IsSymptomMode));
                OnPropertyChanged(nameof(IsFreeTextMode));
            }
        }

        public bool IsFreeTextMode
        {
            get => !_isSymptomMode;
            set => IsSymptomMode = !value;
        }

        public ObservableCollection<SymptomCategory> Categories { get; } = new ObservableCollection<SymptomCategory>();
        public ObservableCollection<Symptom> SelectedSymptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Diagnosis> Diagnoses { get; } = new ObservableCollection<Diagnosis>();
        public ObservableCollection<DiagnosisProbability> DiagnosisProbabilities { get; } = new ObservableCollection<DiagnosisProbability>();
        public ObservableCollection<UsageRecordViewModel> UsageRecords { get; } = new ObservableCollection<UsageRecordViewModel>();
        public ObservableCollection<string> Languages { get; } = new ObservableCollection<string> { "Русский", "English" };

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }

        private string _freeTextComplaint;
        public string FreeTextComplaint
        {
            get => _freeTextComplaint;
            set
            {
                _freeTextComplaint = value;
                OnPropertyChanged(nameof(FreeTextComplaint));
            }
        }

        public Diagnosis SelectedDiagnosis { get; set; }

        public ICommand LoadDataCommand { get; }
        public ICommand TrainModelCommand { get; }
        public ICommand PredictCommand { get; }
        public ICommand SaveUsageCommand { get; }
        public ICommand ToggleSymptomCommand { get; }

        public SymptomSelectionViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            _mlService = new MLService();
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            TrainModelCommand = new RelayCommand(async _ => await _mlService.TrainModelAsync());
            PredictCommand = new RelayCommand(_ => PredictDiagnosis());
            SaveUsageCommand = new RelayCommand(async _ => await SaveUsageAsync());
            ToggleSymptomCommand = new RelayCommand(sym => ToggleSymptom(sym as Symptom));
            SelectedLanguage = Languages.First();
        }

        public async Task ImportDataFromFileAsync(string filePath)
        {
            Categories.Clear();
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                var categoryName = parts[0].Trim();
                var symptomName = parts[1].Trim();
                var category = Categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                if (category == null)
                {
                    category = new SymptomCategory
                    {
                        Id = 0,
                        Name = categoryName,
                        Symptoms = new ObservableCollection<Symptom>()
                    };
                    Categories.Add(category);
                }
                if (!category.Symptoms.Any(s => s.Name.Equals(symptomName, StringComparison.OrdinalIgnoreCase)))
                {
                    category.Symptoms.Add(new Symptom
                    {
                        Id = 0,
                        Name = symptomName,
                        CategoryId = category.Id
                    });
                }
            }
            OnPropertyChanged(nameof(Categories));
        }

        private async Task LoadDataAsync()
        {
            Categories.Clear();
            var cats = await _dbService.GetAllCategoriesAsync();
            var syms = await _dbService.GetAllSymptomsAsync();
            foreach (var c in cats)
            {
                c.Symptoms = new ObservableCollection<Symptom>(syms.Where(s => s.CategoryId == c.Id));
                Categories.Add(c);
            }
            Diagnoses.Clear();
            foreach (var d in await _dbService.GetAllDiagnosesAsync())
                Diagnoses.Add(d);
            DiagnosisProbabilities.Clear();
            UsageRecords.Clear();
            foreach (var r in await _dbService.GetUsageRecordsAsync())
            {
                var symptomsList = await _dbService.GetSymptomsByUsageIdAsync(r.Id);
                var symptomsText = string.Join(", ", symptomsList.Select(s => s.Name));
                var diag = r.DiagnosisId.HasValue
                    ? await _dbService.GetDiagnosisByIdAsync(r.DiagnosisId.Value)
                    : null;
                UsageRecords.Add(new UsageRecordViewModel(r.Id, r.EventTime, symptomsText, diag?.Name));
            }
            OnPropertyChanged(nameof(Categories));
        }

        private void PredictDiagnosis()
        {
            if (IsFreeTextMode)
                ExtractSymptomsFromText();
            var vector = Categories.SelectMany(c => c.Symptoms)
                .Select(s => SelectedSymptoms.Any(ss => ss.Id == s.Id) ? 1f : 0f)
                .ToArray();
            var scores = _mlService.PredictProbabilities(vector);
            DiagnosisProbabilities.Clear();
            var top = Diagnoses.Zip(scores, (d, p) => new DiagnosisProbability(d, p))
                .OrderByDescending(dp => dp.Probability)
                .Take(4);
            foreach (var dp in top)
                DiagnosisProbabilities.Add(dp);
            SelectedDiagnosis = DiagnosisProbabilities.FirstOrDefault()?.Diagnosis;
            OnPropertyChanged(nameof(DiagnosisProbabilities));
            OnPropertyChanged(nameof(SelectedDiagnosis));
        }

        private void ExtractSymptomsFromText()
        {
            SelectedSymptoms.Clear();
            if (string.IsNullOrWhiteSpace(FreeTextComplaint)) return;
            var text = FreeTextComplaint.ToLowerInvariant();
            foreach (var symptom in Categories.SelectMany(c => c.Symptoms))
            {
                var key = symptom.Name.ToLowerInvariant();
                var localized = _locConverter.Convert(new object[] { symptom.Name, SelectedLanguage }, typeof(string), null, CultureInfo.CurrentCulture) as string;
                var locLower = localized?.ToLowerInvariant();
                var patternKey = $"\\b{Regex.Escape(key)}\\b";
                if (Regex.IsMatch(text, patternKey) || (locLower != null && Regex.IsMatch(text, $"\\b{Regex.Escape(locLower)}\\b")))
                    SelectedSymptoms.Add(symptom);
            }
        }

        private async Task SaveUsageAsync()
        {
            var ids = SelectedSymptoms.Select(s => s.Id).ToList();
            int? diagId = SelectedDiagnosis?.Id;
            await _dbService.SaveUsageRecordAsync(ids, diagId);
            await LoadDataAsync();
        }

        private void ToggleSymptom(Symptom symptom)
        {
            if (symptom == null) return;
            var existing = SelectedSymptoms.FirstOrDefault(s => s.Id == symptom.Id);
            if (existing != null)
                SelectedSymptoms.Remove(existing);
            else
                SelectedSymptoms.Add(symptom);
        }
    }

    public class UsageRecordViewModel
    {
        public int Id { get; }
        public DateTime EventTime { get; }
        public string Symptoms { get; }
        public string ResultName { get; }
        public ICommand ExportCommand { get; }

        public UsageRecordViewModel(int id, DateTime eventTime, string symptoms, string resultName)
        {
            Id = id;
            EventTime = eventTime;
            Symptoms = symptoms;
            ResultName = resultName;
            ExportCommand = new RelayCommand(_ => { });
        }
    }
}
