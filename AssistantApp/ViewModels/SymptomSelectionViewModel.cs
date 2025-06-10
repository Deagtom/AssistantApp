using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AssistantApp.Data;
using AssistantApp.Models;
using AssistantApp.Services;

namespace AssistantApp.ViewModels
{
    public class SymptomSelectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly MLService _mlService;

        private bool _isTextMode;
        public bool IsTextMode
        {
            get => _isTextMode;
            set
            {
                _isTextMode = value;
                OnPropertyChanged(nameof(IsTextMode));
                OnPropertyChanged(nameof(IsListMode));
            }
        }

        public bool IsListMode
        {
            get => !_isTextMode;
            set
            {
                _isTextMode = !value;
                OnPropertyChanged(nameof(IsTextMode));
                OnPropertyChanged(nameof(IsListMode));
            }
        }

        private string _complaintText;
        public string ComplaintText
        {
            get => _complaintText;
            set
            {
                _complaintText = value;
                OnPropertyChanged(nameof(ComplaintText));
            }
        }

        private static readonly Dictionary<string, string> SymptomTranslations = new()
        {
            { "Chest pain", "Боль в груди" },
            { "Abdominal pain", "Боль в животе" },
            { "Joint pain", "Боль в суставах" },
            { "Back pain", "Боль в спине" },
            { "Nausea", "Тошнота" },
            { "Diarrhea", "Диарея" },
            { "Vomiting", "Рвота" },
            { "Loss of appetite", "Потеря аппетита" },
            { "Dizziness", "Головокружение" },
            { "Palpitations", "Сердцебиение" },
            { "Headache", "Головная боль" },
            { "Muscle weakness", "Слабость мышц" },
            { "Blurred vision", "Затуманенное зрение" },
            { "Fatigue", "Усталость" },
            { "Sleepiness", "Сонливость" },
            { "Fever", "Лихорадка" },
            { "Cough", "Кашель" },
            { "Shortness of breath", "Одышка" },
            { "Rash", "Сыпь" },
            { "Swelling", "Отёк" },
            { "Itching", "Зуд" },
            { "Chills", "Озноб" },
            { "Sore throat", "Боль в горле" },
            { "Weight loss", "Похудение" },
            { "Excessive thirst", "Чрезмерная жажда" }
        };

        public ObservableCollection<SymptomCategory> Categories { get; } = new ObservableCollection<SymptomCategory>();
        public ObservableCollection<Symptom> SelectedSymptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Diagnosis> Diagnoses { get; } = new ObservableCollection<Diagnosis>();
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
            IsTextMode = false;
            ComplaintText = string.Empty;
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
            var diags = await _dbService.GetAllDiagnosesAsync();
            foreach (var d in diags)
                Diagnoses.Add(d);

            UsageRecords.Clear();
            var records = await _dbService.GetUsageRecordsAsync();
            foreach (var r in records)
            {
                var symptomsList = await _dbService.GetSymptomsByUsageIdAsync(r.Id);
                var symptomsText = string.Join(", ", symptomsList.Select(s => s.Name));
                var diag = r.DiagnosisId.HasValue
                    ? await _dbService.GetDiagnosisByIdAsync(r.DiagnosisId.Value)
                    : null;
                UsageRecords.Add(new UsageRecordViewModel(r.Id, r.EventTime, symptomsText, diag?.Name));
            }
        }

        private void PredictDiagnosis()
        {
            if (IsTextMode)
                ParseSymptomsFromText();

            var vector = Categories
                .SelectMany(c => c.Symptoms)
                .Select(s => SelectedSymptoms.Any(ss => ss.Id == s.Id) ? 1f : 0f)
                .ToArray();
            var label = _mlService.PredictDiagnosis(vector);
            var match = Diagnoses.FirstOrDefault(d => d.Name == label);
            if (match != null)
            {
                SelectedDiagnosis = match;
                OnPropertyChanged(nameof(SelectedDiagnosis));
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

        private void ParseSymptomsFromText()
        {
            SelectedSymptoms.Clear();
            if (string.IsNullOrWhiteSpace(ComplaintText))
                return;

            var text = ComplaintText.ToLowerInvariant();
            foreach (var category in Categories)
            {
                foreach (var symptom in category.Symptoms)
                {
                    var en = symptom.Name.ToLowerInvariant();
                    var ru = SymptomTranslations.ContainsKey(symptom.Name)
                        ? SymptomTranslations[symptom.Name].ToLowerInvariant()
                        : string.Empty;

                    if (text.Contains(en) || (!string.IsNullOrEmpty(ru) && text.Contains(ru)))
                        SelectedSymptoms.Add(symptom);
                }
            }
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