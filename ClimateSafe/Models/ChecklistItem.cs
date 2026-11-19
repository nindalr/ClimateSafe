using System;

namespace ClimateSafe
{
    internal class ChecklistItem
    {
        private int _checklistItemId;
        private string _description;
        private bool _isCompleted;

        public int AssessmentId { get; }
        public int RecommendationId { get; }
        public DateTime? CompletedAt { get; private set; }

        public ChecklistItem(
            int checklistItemId,
            string description,
            int assessmentId = 0,
            int recommendationId = 0,
            bool isCompleted = false,
            DateTime? completedAt = null)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Tindakan checklist tidak boleh kosong.", nameof(description));
            if (isCompleted != completedAt.HasValue)
                throw new ArgumentException("Status checklist harus sesuai dengan waktu penyelesaian.");

            _checklistItemId = checklistItemId;
            _description = description;
            AssessmentId = assessmentId;
            RecommendationId = recommendationId;
            _isCompleted = isCompleted;
            CompletedAt = completedAt;
        }

        public int ChecklistItemId
        {
            get { return _checklistItemId; }
            set { _checklistItemId = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        public void MarkCompleted()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            CompletedAt = DateTime.UtcNow;
        }

        public void MarkIncomplete()
        {
            _isCompleted = false;
            CompletedAt = null;
        }
    }
}
