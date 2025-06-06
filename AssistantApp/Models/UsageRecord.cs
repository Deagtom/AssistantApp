using System;

namespace AssistantApp.Models
{
    public class UsageRecord
    {
        public int Id { get; set; }
        public DateTime EventTime { get; set; }
        public int? DiagnosisId { get; set; }
    }
}