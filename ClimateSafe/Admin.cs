using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class Admin : UserAccount
    {
        private readonly List<User> _managedUsers;

        public Admin(
            int userId,
            string name,
            string email,
            string passwordHash)
            : base(userId, name, email, passwordHash)
        {
            _managedUsers = new List<User>();
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