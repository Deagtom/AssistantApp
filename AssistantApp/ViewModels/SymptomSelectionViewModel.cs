using AssistantApp.Data;
using AssistantApp.Models;
using AssistantApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AssistantApp.ViewModels
{
    public class SymptomSelectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly MLService _mlService;

        public ObservableCollection<SymptomCategory> Categories { get; } = new ObservableCollection<SymptomCategory>();
        public ObservableCollection<Symptom> SelectedSymptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Diagnosis> Diagnoses { get; } = new ObservableCollection<Diagnosis>();
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
        }

        private void PredictDiagnosis()
        {
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
        }

        private void ToggleSymptom(Symptom symptom)
        {
            if (symptom == null)
                return;

            var existing = SelectedSymptoms.FirstOrDefault(s => s.Id == symptom.Id);
            if (existing != null)
                SelectedSymptoms.Remove(existing);
            else
                SelectedSymptoms.Add(symptom);
        }
    }
}