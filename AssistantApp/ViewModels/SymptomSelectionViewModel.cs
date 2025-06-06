using AssistantApp.Data;
using AssistantApp.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AssistantApp.ViewModels
{
    public class SymptomSelectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<SymptomCategory> Categories { get; } = new ObservableCollection<SymptomCategory>();
        public ObservableCollection<Symptom> Symptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Symptom> SelectedSymptoms { get; } = new ObservableCollection<Symptom>();
        public ObservableCollection<Diagnosis> Diagnoses { get; } = new ObservableCollection<Diagnosis>();
        public Diagnosis SelectedDiagnosis { get; set; }
        public ICommand LoadDataCommand { get; }
        public ICommand SaveUsageCommand { get; }

        public SymptomSelectionViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveUsageCommand = new RelayCommand(async _ => await SaveUsageAsync());
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
    }
}
