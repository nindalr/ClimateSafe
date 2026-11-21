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
```

Skrip ini tidak dijalankan secara otomatis saat API dimulai. `000_users.sql`
menyiapkan skema akun baru; pengguna yang sudah ada tidak dihapus. Fungsi akun
belum diimplementasikan pada tahap fondasi.

Gunakan `ClimateSafe.Api.http` untuk memanggil `GET /health` dan `GET /api/hazards`.
Endpoint health menunjukkan bahwa proses API berjalan, sedangkan endpoint hazards
memerlukan koneksi PostgreSQL dan tabel `hazards` yang sudah diinisialisasi.
