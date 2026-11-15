using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ClimateSafe.Auth;

namespace ClimateSafe
{
    // INHERITANCE
    public class Admin : UserAccount
    {
        private readonly List<User> _managedUsers;

        // Constructor lengkap (dari database)
        public Admin(int id, string name, string email, string hash,
                     string role, string status, DateTime createdAt)
            : base(id, name, email, hash, role, status, createdAt)
        {
            _managedUsers = new List<User>();
        }

        // Constructor ringkas (kode ClimateSafe lama)
        public Admin(int userId, string name, string email, string passwordHash)
            : base(userId, name, email, passwordHash, "Admin", "Active", DateTime.Now)
        {
            _managedUsers = new List<User>();
        }

        public override Form CreateMainForm()
        {
            return new FormAdminPanel(this);
        }

        public void ManageUser(int userId)
        {
            foreach (User user in _managedUsers)
            {
                if (user.UserId == userId)
                {
                    return;
                }
            }
        }

        public void AddManagedUser(User user)
        {
            if (user == null)
            {
                return;
            }

            if (!_managedUsers.Contains(user))
            {
                _managedUsers.Add(user);
            }
        }
    }
}