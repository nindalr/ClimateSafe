using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class Answer
    {
        private int _answerId;
        private string _value;
        private int _score;

        private readonly Question _question;

        public Answer(
            int answerId,
            string value,
            int score,
            Question question)
        {
            _answerId = answerId;
            _value = value;
            _score = score;
            _question = question;
        }

        public int AnswerId
        {
            get { return _answerId; }
            set { _answerId = value; }
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public int Score
        {
            get { return _score; }
            set { _score = value; }
        }

        public int CalculateScore()
        {
            return _score;
        }

        public Question Question
        {
            get { return _question; }
        }
    }
}