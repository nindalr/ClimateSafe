using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class Assessment
    {
        private int _assessmentId;
        private DateTime _assessmentDate;
        private double _overallScore;

        private readonly List<Answer> _answers;
        private readonly List<ChecklistItem> _checklistItems;

        public Assessment(int assessmentId, DateTime assessmentDate)
        {
            _assessmentId = assessmentId;
            _assessmentDate = assessmentDate;
            _overallScore = 0.0;

            _answers = new List<Answer>();
            _checklistItems = new List<ChecklistItem>();
        }

        public int AssessmentId
        {
            get { return _assessmentId; }
            set { _assessmentId = value; }
        }

        public DateTime AssessmentDate
        {
            get { return _assessmentDate; }
            set { _assessmentDate = value; }
        }

        public double OverallScore
        {
            get { return _overallScore; }
        }

        public void AddAnswer(Answer answer)
        {
            if (answer == null)
            {
                return;
            }

            if (!_answers.Contains(answer))
            {
                _answers.Add(answer);
            }
        }

        public double CalculateOverallScore()
        {
            if (_answers.Count == 0)
            {
                _overallScore = 0.0;
                return _overallScore;
            }

            int totalScore = 0;
            int maximumScore = _answers.Count * 2;

            foreach (Answer answer in _answers)
            {
                totalScore += answer.CalculateScore();
            }

            _overallScore = maximumScore == 0
                ? 0.0
                : (double)totalScore / maximumScore * 100.0;

            return _overallScore;
        }

        public void AddChecklistItem(ChecklistItem item)
        {
            if (item == null)
            {
                return;
            }

            if (!_checklistItems.Contains(item))
            {
                _checklistItems.Add(item);
            }
        }

        public void CompleteChecklistItem(int itemId)
        {
            foreach (ChecklistItem item in _checklistItems)
            {
                if (item.ChecklistItemId == itemId)
                {
                    item.MarkCompleted();
                    return;
                }
            }
        }

        public IReadOnlyList<Answer> GetAnswers()
        {
            return _answers.AsReadOnly();
        }

        public IReadOnlyList<ChecklistItem> GetChecklistItems()
        {
            return _checklistItems.AsReadOnly();
        }
    }
}