using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public virtual bool Login(string email, string passwordHash)
        {
            return _email == email && _passwordHash == passwordHash;
        }

        public virtual void Logout()
        {
            // Logout behavior can be connected to the application's
            // authentication/session management.
        }
    }
}