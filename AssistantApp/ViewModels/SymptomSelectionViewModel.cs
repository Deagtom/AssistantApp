using AssistantApp.Data;
using AssistantApp.Models;
using System.Collections.ObjectModel;
using AssistantApp.Services;
using System.Linq;
using System.Windows.Input;

namespace AssistantApp.ViewModels
{
    public class SymptomSelectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly MLService _mlService;

        public ObservableCollection<SymptomCategory> Categories { get; } = new ObservableCollection<SymptomCategory>();
        public ObservableCollection<Symptom> Symptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Symptom> SelectedSymptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Diagnosis> Diagnoses { get; } = new ObservableCollection<Diagnosis>();
        public Diagnosis SelectedDiagnosis { get; set; }
        public ICommand LoadDataCommand { get; }
        public ICommand SaveUsageCommand { get; }
        public ICommand TrainModelCommand { get; }
        public ICommand PredictCommand { get; }
        public SymptomSelectionViewModel(DatabaseService dbService, MLService mlService)
        {
            _dbService = dbService;
            _mlService = mlService;
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveUsageCommand = new RelayCommand(async _ => await SaveUsageAsync());
            TrainModelCommand = new RelayCommand(async _ => await TrainModelAsync());
            PredictCommand = new RelayCommand(async _ => await PredictAsync());
        }

        private async Task LoadDataAsync()
        {
            Categories.Clear();
            var categoriesFromDb = await _dbService.GetAllCategoriesAsync();
            foreach (var cat in categoriesFromDb)
                Categories.Add(cat);

            Symptoms.Clear();
            var symptomsFromDb = await _dbService.GetAllSymptomsAsync();
            foreach (var sym in symptomsFromDb)
                Symptoms.Add(sym);

            foreach (var cat in Categories)
            {
                var matched = Symptoms.Where(s => s.CategoryId == cat.Id);
                cat.Symptoms = new ObservableCollection<Symptom>(matched);
            }

            Diagnoses.Clear();
            var diagnosesFromDb = await _dbService.GetAllDiagnosesAsync();
            foreach (var diag in diagnosesFromDb)
                Diagnoses.Add(diag);
        }

        private async Task SaveUsageAsync()
        {
            var symptomIds = SelectedSymptoms.Select(s => s.Id).ToList();
            int? diagId = SelectedDiagnosis?.Id;
            await _dbService.SaveUsageRecordAsync(symptomIds, diagId);
        }

        private async Task TrainModelAsync()
        {
            await _mlService.TrainModelAsync();
        }

        private async Task PredictAsync()
        {
            var symptomIds = SelectedSymptoms.Select(s => s.Id).ToList();
            var result = await _mlService.PredictDiagnosisAsync(symptomIds);
            if (result != null)
            {
                SelectedDiagnosis = Diagnoses.FirstOrDefault(d => d.Name == result);
                OnPropertyChanged(nameof(SelectedDiagnosis));
            }
        }
    }
}
