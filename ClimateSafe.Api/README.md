# ClimateSafe API

API menggunakan .NET 10 dan PostgreSQL. Atur connection string sebagai environment
variable di lingkungan server atau terminal pengembangan. Contoh PowerShell:

```powershell
$env:ConnectionStrings__ClimateSafe = 'Host=localhost;Port=5432;Database=climatesafe;Username=postgres;Password=PASSWORD_ANDA'
dotnet run --project ClimateSafe.Api
```

Untuk database baru, buat database terlebih dahulu, lalu jalankan skrip SQL secara
berurutan dengan `psql` dan koneksi ke database tersebut:

```powershell
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/000_users.sql
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/001_assessment_checklist.sql
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/002_assessment_history.sql
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/003_account_sessions.sql
```

Skrip ini tidak dijalankan secara otomatis saat API dimulai. `000_users.sql`
menyiapkan skema akun baru; pengguna yang sudah ada tidak dihapus. Migrasi 003
membuat tabel sesi dan indeks email yang tidak membedakan huruf besar dan kecil.
Email duplikat pada database lama perlu diselesaikan sebelum menjalankan migrasi.

Gunakan `ClimateSafe.Api.http` untuk memanggil `GET /health` dan `GET /api/hazards`.
Endpoint health menunjukkan bahwa proses API berjalan, sedangkan endpoint hazards
memerlukan koneksi PostgreSQL dan tabel `hazards` yang sudah diinisialisasi.

Endpoint akun menyediakan `POST /api/account/register`, `POST /api/account/login`,
`GET /api/account/me`, `PATCH /api/account/me`, dan `POST /api/account/logout`.
Login menghasilkan token sesi yang berlaku delapan jam. Kirim token menggunakan
header `Authorization: Bearer <token>` untuk endpoint yang memerlukan login.
Pendaftaran selalu membuat akun berperan User. Akun Admin harus disiapkan oleh
pengelola database; endpoint admin tersedia di `/api/admin/users`.
Gunakan HTTPS saat mengirim password dan token melalui jaringan.
