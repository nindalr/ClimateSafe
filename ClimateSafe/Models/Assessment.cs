using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateSafe
{
    internal class Assessment
    {
        private const int MaximumAnswerScore = 3;
        private int _assessmentId;
        private DateTime _assessmentDate;
        private double _overallScore;
        private string _level = string.Empty;

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

        public string Level
        {
            get { return _level; }
        }

        public void AddAnswer(Answer answer)
        {
            SubmitAnswer(answer);
        }

        public double CalculateOverallScore()
        {
            if (_answers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Assessment belum memiliki jawaban.");
            }

            int totalScore = 0;

            foreach (Answer answer in _answers)
            {
                totalScore += answer.CalculateScore();
            }

            int maximumScore = _answers.Count * MaximumAnswerScore;
            _overallScore = (double)totalScore / maximumScore * 100.0;
            _level = DetermineLevel(_overallScore);
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
            // Lindungi objek di dalam koleksi dari perubahan oleh pemanggil.
            return _answers.Select(CopyAnswer).ToList().AsReadOnly();
        }

        public IReadOnlyList<ChecklistItem> GetChecklistItems()
        {
            return _checklistItems.AsReadOnly();
        }

        public void SubmitAnswer(Answer answer)
        {
            if (answer == null)
            {
                throw new ArgumentNullException(nameof(answer));
            }

            if (answer.Question == null)
            {
                throw new ArgumentException(
                    "Jawaban harus memiliki pertanyaan.",
                     nameof(answer)
                     );
            }

            ValidateScore(answer.Score);

            if (_answers.Any(existing => 
                existing.Question.QuestionId 
                == answer.Question.QuestionId))
            {
                throw new InvalidOperationException("Pertanyaan ini sudah dijawab.");
            }

            // Answer dan Question masih dapat diubah; simpan salinan milik assessment.
            _answers.Add(CopyAnswer(answer));

            // Hasil sebelumnya tidak berlaku setelah jawaban bertambah.
            _overallScore = 0.0;
            _level = string.Empty;
        }

        private static Answer CopyAnswer(Answer answer)
        {
            Question question = new Question(answer.Question.QuestionId, answer.Question.QuestionText);
            return new Answer(answer.AnswerId, answer.Value, answer.Score, question);
        }

        private static void ValidateScore(int score)
        {
            if (score < 0 || score > MaximumAnswerScore)
            {
                throw new ArgumentOutOfRangeException(nameof(score), score,
                    "Skor jawaban harus berada dalam rentang 0 sampai 3.");
            }
        }

        private static string DetermineLevel(double score)
        {
            if (score >= 80) return "Siap";
            if (score >= 40) return "Cukup Siap";
            return "Belum Siap";
        }
    }
}
