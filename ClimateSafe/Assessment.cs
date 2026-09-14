using System;
using System.Collections.Generic;

namespace ClimateSafe
{
    internal class Assessment
    {
        private int _assessmentId;
        private DateTime _assessmentDate;
        private double _overallScore;

        private Hazard _hazard;
        private readonly List<Answer> _answers;
        private readonly List<ChecklistItem> _checklistItems;

        public Assessment(int assessmentId, DateTime assessmentDate, Hazard hazard = null)
        {
            _assessmentId = assessmentId;
            _assessmentDate = assessmentDate;
            _hazard = hazard;
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
            set { _overallScore = value; }
        }

        public Hazard Hazard
        {
            get { return _hazard; }
            set { _hazard = value; }
        }

        public void AddAnswer(Answer answer)
        {
            if (answer != null)
            {
                _answers.Add(answer);
            }
        }

        public double CalculateOverallScore()
        {
            return _overallScore;
        }

        public List<Answer> Answers
        {
            get { return _answers; }
        }

        public List<ChecklistItem> ChecklistItems
        {
            get { return _checklistItems; }
        }
    }
}