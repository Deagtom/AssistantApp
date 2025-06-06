using System.Collections.ObjectModel;

namespace AssistantApp.Models
{
    public class SymptomCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ObservableCollection<Symptom> Symptoms { get; set; } = new ObservableCollection<Symptom>();
    }
}