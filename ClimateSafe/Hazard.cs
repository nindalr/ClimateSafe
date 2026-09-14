using System;
using System.Collections.Generic;

namespace ClimateSafe
{
    internal class Hazard
    {
        private int _hazardId;
        private string _name;

        private readonly List<Question> _questions;

        public Hazard(int hazardId, string name)
        {
            _hazardId = hazardId;
            _name = name;
            _questions = new List<Question>();
        }

        public int HazardId
        {
            get { return _hazardId; }
            set { _hazardId = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public List<Question> GetQuestions()
        {
            return _questions;
        }

        public List<Question> Questions
        {
            get { return _questions; }
        }
    }
}
