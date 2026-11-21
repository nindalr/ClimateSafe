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
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/004_question_catalog.sql
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/005_assessment_submission.sql
```

Skrip ini tidak dijalankan secara otomatis saat API dimulai. `000_users.sql`
menyiapkan skema akun baru; pengguna yang sudah ada tidak dihapus. Migrasi 003
membuat tabel sesi dan indeks email yang tidak membedakan huruf besar dan kecil.
Email duplikat pada database lama perlu diselesaikan sebelum menjalankan migrasi.
Migrasi 004 menambahkan pilihan jawaban dan katalog awal. Pertanyaan yang sudah
ada untuk hazard dan dimensi yang sama dipertahankan, termasuk ID dan teksnya.
ID baru mengikuti format `hazard_id * 100 + dimension_id`; jika ID tersebut sudah
dipakai oleh kombinasi lain, migrasi gagal dan transaksi dibatalkan untuk ditinjau.
Migrasi 005 menambahkan katalog rekomendasi awal dan penyimpanan ID pilihan untuk
mendeteksi pengiriman ulang. Rekomendasi yang sudah ada di katalog tidak ditimpa.

Gunakan `ClimateSafe.Api.http` untuk memanggil `GET /health` dan `GET /api/hazards`.
Endpoint health menunjukkan bahwa proses API berjalan, sedangkan endpoint hazards
memerlukan koneksi PostgreSQL dan tabel `hazards` yang sudah diinisialisasi.
`GET /api/hazards/{id}/questions` mengembalikan pertanyaan, keterangan, dan pilihan
jawaban yang diurutkan. Bobot pilihan disimpan di server dan tidak dikirim ke client.

Endpoint akun menyediakan `POST /api/account/register`, `POST /api/account/login`,
`GET /api/account/me`, `PATCH /api/account/me`, dan `POST /api/account/logout`.
Login menghasilkan token sesi yang berlaku delapan jam. Kirim token menggunakan
header `Authorization: Bearer <token>` untuk endpoint yang memerlukan login.
Pendaftaran selalu membuat akun berperan User. Akun Admin harus disiapkan oleh
pengelola database; endpoint admin tersedia di `/api/admin/users`.
Gunakan HTTPS saat mengirim password dan token melalui jaringan.

Riwayat dan checklist memerlukan token login yang sama:

- `GET /api/assessments` menampilkan daftar asesmen dan jumlah checklist.
- `GET /api/assessments/{id}` menampilkan hasil, jawaban, skor per dimensi, dan checklist.
- `GET /api/assessments/{id}/checklist` menampilkan checklist satu asesmen.
- `PATCH /api/checklist/{id}` mengubah status dengan body `{ "isCompleted": true }`
  atau `{ "isCompleted": false }`.

Data hanya tersedia untuk pemilik asesmen. Target dimensi pada riwayat mengikuti
nilai database saat ini. Skor per dimensi dihitung dengan skor maksimum jawaban 3,
sesuai aturan aplikasi desktop saat ini.

`POST /api/assessments` menerima token login dan JSON berisi `submissionKey` (UUID),
`hazardId`, serta `optionIds` yang mencakup satu pilihan untuk setiap pertanyaan.
Ambil ID pilihan dari katalog hazard. Server menentukan bobot, skor, dan rekomendasi.
Gunakan kunci yang sama jika mengulang pengiriman jawaban yang sama; jika jawaban
atau hazard berbeda, gunakan kunci baru. Pengiriman ulang mengembalikan asesmen yang
sudah disimpan. Kunci asesmen lama yang tidak memiliki catatan ID pilihan tidak
dapat digunakan untuk pengiriman ulang melalui endpoint ini.
