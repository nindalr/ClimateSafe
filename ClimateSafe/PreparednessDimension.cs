using System;
using System.Collections.Generic;

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
            return 0.0;
        }

        public bool IdentifyGap(double score)
        {
            return false;
        }

        public List<Question> Questions
        {
            get { return _questions; }
        }

        public List<Recommendation> Recommendations
        {
            get { return _recommendations; }
        }
    }
}