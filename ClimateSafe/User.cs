using System;
using System.Collections.Generic;

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

        public void PerformAssessment()
        {
        }

        public List<Assessment> ViewHistory()
        {
            return _assessments;
        }

        public void UpdateChecklist(int itemId)
        {
        }
    }
}