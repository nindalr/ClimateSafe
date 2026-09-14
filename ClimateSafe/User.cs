using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class User : UserAccount
    {
        private readonly List<Assessment> _assessments;

        public User(
            int userId,
            string name,
            string email,
            string passwordHash)
            : base(userId, name, email, passwordHash)
        {
            _assessments = new List<Assessment>();
        }

        public void PerformAssessment(Assessment assessment)
        {
            if (assessment == null)
            {
                return;
            }

            if (!_assessments.Contains(assessment))
            {
                _assessments.Add(assessment);
            }
        }

        public List<Assessment> ViewHistory()
        {
            return new List<Assessment>(_assessments);
        }

        public void UpdateChecklist(int itemId)
        {
            foreach (Assessment assessment in _assessments)
            {
                assessment.CompleteChecklistItem(itemId);
            }
        }
    }
}