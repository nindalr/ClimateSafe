using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ClimateSafe.Auth;

namespace ClimateSafe
{
    // INHERITANCE
    public class User : UserAccount
    {
        private readonly List<Assessment> _assessments;

        // Constructor lengkap (dari database)
        public User(int id, string name, string email, string hash,
                    string role, string status, DateTime createdAt)
            : base(id, name, email, hash, role, status, createdAt)
        {
            _assessments = new List<Assessment>();
        }

        // Constructor ringkas (kode ClimateSafe lama)
        public User(int userId, string name, string email, string passwordHash)
            : base(userId, name, email, passwordHash)
        {
            _assessments = new List<Assessment>();
        }

        public override Form CreateMainForm()
        {
            Session.CurrentUser = this;   // isi dulu sebelum dashboard dibuat
            Nav.Reset();                  // buang dashboard lama (cache) kalau ganti akun
            return new FrmDashboard();
        }

        internal void PerformAssessment(Assessment assessment)
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

        internal List<Assessment> ViewHistory()
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