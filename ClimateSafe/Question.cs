using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class Question
    {
        private int _questionId;
        private string _questionText;

        public Question(int questionId, string questionText)
        {
            _questionId = questionId;
            _questionText = questionText;
        }

        public int QuestionId
        {
            get { return _questionId; }
            set { _questionId = value; }
        }

        public string QuestionText
        {
            get { return _questionText; }
            set { _questionText = value; }
        }
    }
}