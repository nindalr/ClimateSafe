using System;

namespace ClimateSafe.Auth
{
    public class AuthService
    {
        private readonly UserRepository _repo = new UserRepository();

        public bool Register(string name, string email, string password)
        {
            name = name.Trim();
            email = email.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nama tidak boleh kosong");
            if (!email.Contains("@") || !email.Contains("."))
                throw new ArgumentException("Format email tidak valid");
            if (password.Length < 6)
                throw new ArgumentException("Password minimal 6 karakter");

            return _repo.Register(name, email, PasswordHasher.Hash(password));
        }

        public UserAccount? Login(string email, string password)
        {
            UserAccount? user = _repo.GetByEmail(email.Trim().ToLower());
            if (user != null && user.Login(password)) return user;
            return null;
        }
    }
}
