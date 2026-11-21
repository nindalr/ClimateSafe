using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateSafe
{
    internal class PreparednessDimension
    {
        private int _dimensionId;
        private string _name;
        private readonly double _passingThreshold;

        private readonly List<Question> _questions;
        private readonly List<Recommendation> _recommendations;

        public PreparednessDimension(int dimensionId, string name, double passingThreshold)
        {
            if (!double.IsFinite(passingThreshold) || passingThreshold <= 0 || passingThreshold > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(passingThreshold),
                    "Ambang dimensi harus lebih dari 0 dan maksimal 100.");
            }

            _dimensionId = dimensionId;
            _name = name;
            _passingThreshold = passingThreshold;

            _questions = new List<Question>();
            _recommendations = new List<Recommendation>();
        }

        public int DimensionId
        {
            get { return _dimensionId; }
            set { _dimensionId = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public double CalculateScore(Assessment assessment)
        {
            ArgumentNullException.ThrowIfNull(assessment);
            if (_questions.Count == 0)
            {
                throw new InvalidOperationException("Dimensi belum memiliki pertanyaan.");
            }

            var questionIds = _questions.Select(question => question.QuestionId).ToHashSet();
            var answers = assessment.GetAnswers()
                .Where(answer => questionIds.Contains(answer.Question.QuestionId)).ToList();
            if (answers.Count != questionIds.Count)
            {
                throw new InvalidOperationException("Jawaban untuk dimensi belum lengkap.");
            }

            return (double)answers.Sum(answer => answer.CalculateScore()) /
                (questionIds.Count * AssessmentRules.MaximumAnswerScore) * 100.0;
        }

        public bool IdentifyGap(double score)
        {
            if (!double.IsFinite(score) || score < 0 || score > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(score),
                    "Skor dimensi harus berada dalam rentang 0 sampai 100.");
            }

            return score < _passingThreshold;
        }

        public void AddQuestion(Question question)
        {
            if (question == null)
            {
                return;
            }

            if (!_questions.Contains(question))
            {
                _questions.Add(question);
            }
        }

        public void AddRecommendation(Recommendation recommendation)
        {
            if (recommendation == null)
            {
                return;
            }

            if (!_recommendations.Contains(recommendation))
            {
                _recommendations.Add(recommendation);
            }
        }
    }
}
