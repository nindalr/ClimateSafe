using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateSafe
{
    internal sealed class AssessmentScoringService
    {
        private static readonly int[] AnswerWeights = { 3, 2, 1, 0 };

        public IReadOnlyList<DimensionGap> IdentifyGaps(
            Assessment assessment, IReadOnlyList<string> dimensionNames)
        {
            ArgumentNullException.ThrowIfNull(assessment);
            ArgumentNullException.ThrowIfNull(dimensionNames);

            var answers = assessment.GetAnswers()
                .ToDictionary(answer => answer.Question.QuestionId);
            if (dimensionNames.Count == 0 || dimensionNames.Count != answers.Count)
            {
                throw new ArgumentException("Jumlah dimensi harus sesuai dengan pertanyaan asesmen.");
            }

            var gaps = new List<DimensionGap>();
            for (int i = 0; i < dimensionNames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(dimensionNames[i]) ||
                    !answers.TryGetValue(i + 1, out Answer answer))
                {
                    throw new ArgumentException("Nama dimensi dan pertanyaan terkait harus tersedia.");
                }

                var dimension = new PreparednessDimension(i + 1, dimensionNames[i]);
                dimension.AddQuestion(answer.Question);
                double score = dimension.CalculateScore(assessment);
                if (dimension.IdentifyGap(score))
                {
                    gaps.Add(new DimensionGap(dimension.DimensionId, dimension.Name, score));
                }
            }

            return gaps.AsReadOnly();
        }

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
