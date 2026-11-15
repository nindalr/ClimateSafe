using System;
using System.Windows.Forms;
using ClimateSafe.Auth;

namespace ClimateSafe
{
    // ENCAPSULATION + POLYMORPHISM
    public abstract class UserAccount
    {
        private string passwordHash;

        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; private set; }
        public string Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public string PasswordHash
        {
            get { return passwordHash; }
            set { passwordHash = value; }
        }

        // Constructor lengkap (dipakai UserRepository / login)
        protected UserAccount(int id, string name, string email, string hash,
                              string role, string status, DateTime createdAt)
        {
            UserId = id;
            Name = name;
            Email = email;
            passwordHash = hash;
            Role = role;
            Status = status;
            CreatedAt = createdAt;
        }

        // Constructor ringkas (kompatibel dengan kode ClimateSafe lama)
        protected UserAccount(int userId, string name, string email, string passwordHash)
            : this(userId, name, email, passwordHash, "User", "Active", DateTime.Now)
        {
        }

        // Login dengan password asli (dipakai AuthService)
        public bool Login(string plainPassword)
        {
            if (Status != "Active") return false;
            return PasswordHasher.Verify(plainPassword, passwordHash);
        }

        // Login lama versi ClimateSafe
        public virtual bool Login(string email, string passwordHash)
        {
            return Email == email && this.passwordHash == passwordHash;
        }

        public virtual void Logout()
        {
        }

        // POLYMORPHISM
        public abstract Form CreateMainForm();
    }
}