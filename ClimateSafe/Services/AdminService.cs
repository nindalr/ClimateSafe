using System;
using System.Data;

namespace ClimateSafe.Auth
{
    internal sealed class AdminService
    {
        private readonly AdminRepository repository;

        public AdminService(AdminRepository? repository = null)
        {
            this.repository = repository ?? new AdminRepository();
        }

        private static int Authorize(UserAccount? actor)
        {
            if (actor is not Admin || actor.UserId <= 0 || actor.Role != "Admin" || actor.Status != "Active")
                throw new UnauthorizedAccessException("Hanya admin aktif yang dapat mengelola akun.");
            return actor.UserId;
        }

        private static void ValidateTarget(int userId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        }

        public DataTable GetUsers(UserAccount actor) => repository.GetUsers(Authorize(actor));

        public bool UpdateName(UserAccount actor, int userId, string name)
        {
            int adminId = Authorize(actor);
            ValidateTarget(userId);
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nama tidak boleh kosong.", nameof(name));
            return repository.UpdateName(adminId, userId, name.Trim());
        }

        public string? ToggleStatus(UserAccount actor, int userId)
        {
            int adminId = Authorize(actor);
            ValidateTarget(userId);
            if (adminId == userId) throw new InvalidOperationException("Tidak dapat mengubah status akun sendiri.");
            return repository.ToggleStatus(adminId, userId);
        }

        public bool DeleteUser(UserAccount actor, int userId)
        {
            int adminId = Authorize(actor);
            ValidateTarget(userId);
            if (adminId == userId) throw new InvalidOperationException("Tidak dapat menghapus akun sendiri.");
            return repository.DeleteUser(adminId, userId);
        }

        public bool ResetPassword(UserAccount actor, int userId, string password)
        {
            int adminId = Authorize(actor);
            ValidateTarget(userId);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Password minimal 6 karakter dan tidak boleh kosong.", nameof(password));
            return repository.ResetPassword(adminId, userId, PasswordHasher.Hash(password));
        }
    }
}
