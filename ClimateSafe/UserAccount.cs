using System;

namespace ClimateSafe
{
    internal abstract class UserAccount
    {
        private int _userId;
        private string _name;
        private string _email;
        private string _passwordHash;

        protected UserAccount(
            int userId,
            string name,
            string email,
            string passwordHash)
        {
            _userId = userId;
            _name = name;
            _email = email;
            _passwordHash = passwordHash;
        }

        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = value; }
        }

        public virtual bool Login()
        {
            return false;
        }

        public virtual void Logout()
        {
        }
    }
}