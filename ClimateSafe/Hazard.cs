using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return new List<Question>(_questions);
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
    }
}
