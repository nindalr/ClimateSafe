# ClimateSafe API

API menggunakan .NET 10 dan PostgreSQL.

## Railway

Dockerfile di root membangun API dan `ClimateSafe.Core` menggunakan multi-stage
build, lalu menjalankan API pada HTTP port 8080. Atur port publik service Railway
ke 8080 (`PORT=8080`) dan berikan connection string database melalui variabel
`ConnectionStrings__ClimateSafe`; password dapat diberikan melalui `PGPASSWORD`.
Jangan simpan kredensial dalam image atau repository. Database Supabase tetap
memerlukan migrasi 000 sampai 007 yang dijalankan terpisah.

Untuk koneksi Supabase gunakan SSL dengan verifikasi identitas server. Jika root
CA tidak dipercaya oleh container, sediakan sertifikat CA yang benar di lingkungan
service dan atur `Root Certificate`; path sertifikat Windows di komputer lokal
tidak berlaku di container Linux. Uji `/health` untuk proses API dan `/api/hazards`
untuk akses database. Konfigurasi produksi dan sertifikat harus diuji pada Railway.

Perhitungan skor dan prioritas menggunakan `ClimateSafe.Core`, yang juga
direferensikan aplikasi desktop. Katalog guest bawaan versi `initial-001` berasal
dari data awal migrasi 001, 004, dan 005. Katalog API online tetap dibaca dari
PostgreSQL; perubahan pada salah satu salinan tidak otomatis mengubah yang lain.
ID pilihan database tidak digunakan sebagai ID pilihan katalog guest.

Atur connection string sebagai environment variable di lingkungan server atau
terminal pengembangan. Contoh PowerShell:

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
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/006_education_contents.sql
psql -U postgres -d climatesafe -v ON_ERROR_STOP=1 -f ClimateSafe.Api/Data/Migrations/007_multiple_education_contents.sql
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
Migrasi 006 menyiapkan konten edukasi dan 007 memungkinkan beberapa materi per hazard tanpa menghapus materi yang sudah ada. Materi awal tidak diisi secara otomatis.

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

`GET /api/admin/monitoring` tersedia untuk admin aktif dan mengembalikan jumlah
akun berperan User, jumlah akun User aktif, jumlah asesmen, dan daftar asesmen.
Parameter `period` menerima `today` (default), `7days`, atau `30days`.
Rentang menggunakan hari kalender WIB dan mencakup hari ini sampai waktu
permintaan; `7days` dan `30days` mencakup 7 atau 30 tanggal termasuk hari ini.
Parameter `hazardId` opsional hanya memfilter jumlah dan daftar asesmen, bukan
jumlah akun. Parameter `page` dimulai dari 1 dan setiap halaman memuat paling
banyak 50 asesmen. Jumlah dan daftar diambil dari snapshot database yang sama.
Data bersumber dari asesmen yang telah tersimpan; ini bukan audit log aktivitas.

Riwayat dan checklist memerlukan token login yang sama:

- `GET /api/assessments` menampilkan daftar asesmen dan jumlah checklist.
- `GET /api/assessments/sessions` menampilkan daftar asesmen beserta checklist-nya.
- `GET /api/assessments/{id}` menampilkan hasil, jawaban, skor per dimensi, dan checklist.
- `GET /api/assessments/{id}/checklist` menampilkan checklist satu asesmen.
- `PATCH /api/checklist/{id}` mengubah status dengan body `{ "isCompleted": true }`
  atau `{ "isCompleted": false }`.

Data hanya tersedia untuk pemilik asesmen. Target dimensi pada riwayat mengikuti
nilai database saat ini. Skor per dimensi dihitung dengan skor maksimum jawaban 3,
sesuai aturan aplikasi desktop saat ini.
Detail juga menyertakan rekomendasi yang dihitung ulang berdasarkan target dan
katalog rekomendasi saat ini. Checklist menyimpan deskripsi saat asesmen dibuat.

`POST /api/assessments` menerima token login dan JSON berisi `submissionKey` (UUID),
`hazardId`, serta `optionIds` yang mencakup satu pilihan untuk setiap pertanyaan.
Ambil ID pilihan dari katalog hazard. Server menentukan bobot, skor, dan rekomendasi.
Gunakan kunci yang sama jika mengulang pengiriman jawaban yang sama; jika jawaban
atau hazard berbeda, gunakan kunci baru. Pengiriman ulang mengembalikan asesmen yang
sudah disimpan. Kunci asesmen lama yang tidak memiliki catatan ID pilihan tidak
dapat digunakan untuk pengiriman ulang melalui endpoint ini.

## Konten edukasi

- `GET /api/education/hazards/{hazardId}` menyediakan daftar materi yang terpublikasi; daftar kosong dikembalikan jika belum ada.
- `GET /api/admin/education?hazardId={hazardId}` dan `GET /api/admin/education/{contentId}` membaca
  daftar atau detail draft beserta salinan yang sedang terpublikasi.
- `POST /api/admin/education` membuat draft dengan `hazardId`, `title`, `body`, `sourceName`, dan `sourceUrl`.
- `PUT /api/admin/education/{contentId}` memperbarui draft dengan `title`, `body`,
  `sourceName`, dan `sourceUrl`. Judul dan isi wajib diisi. Tautan sumber opsional
  harus menggunakan HTTP atau HTTPS.
- `POST /api/admin/education/{contentId}/publish` mempublikasikan draft terkini.
- `POST /api/admin/education/{contentId}/unpublish` menarik publikasi, tetapi tetap
  menyimpan draft untuk disunting atau dipublikasikan kembali.

Semua endpoint admin membutuhkan token admin aktif. Setiap hazard dapat memiliki
beberapa materi. Menyimpan draft tidak mengubah salinan yang sudah
terpublikasi sampai admin mempublikasikannya kembali. `updatedBy` mencatat admin
terakhir dan menjadi null jika akun admin tersebut dihapus.
