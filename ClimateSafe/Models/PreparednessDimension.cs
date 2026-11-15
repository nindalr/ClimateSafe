using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class PreparednessDimension
    {
        private int _dimensionId;
        private string _name;

        private readonly List<Question> _questions;
        private readonly List<Recommendation> _recommendations;

        public PreparednessDimension(int dimensionId, string name)
        {
            _dimensionId = dimensionId;
            _name = name;

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
            if (assessment == null)
            {
                return 0.0;
            }

            return assessment.CalculateOverallScore();
        }

        public bool IdentifyGap(double score)
        {
            return score < 75.0;
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