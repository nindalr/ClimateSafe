using System;

namespace ClimateSafe
{
    internal class Admin : UserAccount
    {
        public Admin(
            int userId,
            string name,
            string email,
            string passwordHash)
            : base(userId, name, email, passwordHash)
        {
        }

        public void ManageUser(int userId)
        {
        }
    }
}