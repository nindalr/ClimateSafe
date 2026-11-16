using System;
using System.Collections.Generic;

namespace ClimateSafe
{
    internal sealed class AssessmentScoringService
    {
        private static readonly int[] AnswerWeights = { 3, 2, 1, 0 };

        public Assessment Evaluate(
            IReadOnlyList<string> questions,
            IReadOnlyList<string[]> answerOptions,
            IReadOnlyList<int> selectedAnswers)
        {
            ArgumentNullException.ThrowIfNull(questions);
            var hazard = new Hazard(0, string.Empty);
            for (int i = 0; i < questions.Count; i++)
            {
                hazard.AddQuestion(new Question(i + 1, questions[i]));
            }

            return Evaluate(hazard, answerOptions, selectedAnswers);
        }

        public Assessment Evaluate(
            Hazard hazard,
            IReadOnlyList<string[]> answerOptions,
            IReadOnlyList<int> selectedAnswers)
        {
            ArgumentNullException.ThrowIfNull(hazard);
            ArgumentNullException.ThrowIfNull(answerOptions);
            ArgumentNullException.ThrowIfNull(selectedAnswers);

            List<Question> questions = hazard.GetQuestions();

            if (questions.Count == 0 || questions.Count != answerOptions.Count ||
                questions.Count != selectedAnswers.Count)
            {
                throw new ArgumentException("Jumlah pertanyaan, " +
                    "opsi, dan jawaban harus sama dan tidak kosong.");
            }

            Assessment assessment = new Assessment(0, DateTime.Now);
            for (int i = 0; i < questions.Count; i++)
            {
                int selected = selectedAnswers[i];
                if (selected < 0 || selected >= AnswerWeights.Length)
                {
                    throw new ArgumentException("Lengkapi semua " +
                        "pertanyaan dengan satu pilihan yang valid.");
                }

                if (string.IsNullOrWhiteSpace(questions[i].QuestionText) || 
                    answerOptions[i] == null ||
                    answerOptions[i].Length != AnswerWeights.Length ||
                    string.IsNullOrWhiteSpace(answerOptions[i][selected]))
                {
                    throw new ArgumentException("Pertanyaan dan empat opsi " +
                        "jawaban harus tersedia dengan benar.");
                }

                Question question = questions[i];
                assessment.SubmitAnswer(new Answer(i + 1, answerOptions[i][selected],
                    AnswerWeights[selected], question));
            }

            assessment.CalculateOverallScore();
            return assessment;
        }
    }
}
