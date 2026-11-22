# ClimateSafe

### Personal Climate Disaster Preparedness Assessment & Action Planner

**Kelompok 17**

- **Ketua Kelompok:** Nathanael Satya Saputra - 24/534424/TK/59236
- **Anggota 1:** Nathanael Satya Saputra - 24/534424/TK/59236
- **Anggota 2:** Zahra Elfatima - 24/535709/TK/59448
- **Anggota 3:** Ninda Alifa Rachmayanti - 24/545484/TK/60679

ClimateSafe adalah aplikasi desktop yang membantu pengguna mengetahui tingkat kesiapan dalam menghadapi bencana akibat perubahan iklim. Aplikasi ini menyediakan asesmen untuk beberapa jenis bencana, menampilkan bagian yang masih perlu ditingkatkan, dan memberikan rekomendasi tindakan yang dapat dilakukan.

## Panduan Instalasi dan Penggunaan

Panduan ini menjelaskan cara menginstal, mengatur database, dan menggunakan aplikasi ClimateSafe. Ikuti langkah-langkah berikut secara berurutan.

### Langkah 1. Menyiapkan database dan API

Untuk pengembangan lokal, siapkan PostgreSQL dan jalankan migrasi 000 sampai 007 sesuai petunjuk di `ClimateSafe.Api/README.md`. Untuk database kosong, `database/setup.bat` juga dapat menyiapkan skema awal dan menjalankan migrasi 002 sampai 007. Perintah `setup.bat reset` menghapus database beserta seluruh datanya.

Pada terminal pertama, atur connection string untuk API dan jalankan API:

```powershell
$env:ConnectionStrings__ClimateSafe = 'Host=localhost;Port=5432;Database=climatesafe;Username=postgres;Password=PASSWORD_ANDA'
dotnet run --project ClimateSafe.Api --launch-profile http
```

Sesuaikan koneksi dengan database Anda dan biarkan API berjalan. Buka `http://localhost:5028/health` untuk memeriksa proses API.

### Langkah 2. Menjalankan aplikasi desktop

Pada terminal kedua, jalankan:

```powershell
$env:CLIMATESAFE_API_URL = 'http://localhost:5028'
dotnet run --project ClimateSafe/ClimateSafe.csproj
```

Alamat localhost dipakai secara default jika variabel tidak diatur. Untuk server selain localhost, gunakan HTTPS. Pengguna desktop tidak perlu memasukkan kredensial PostgreSQL.

### Langkah 4. Login ke Aplikasi

ClimateSafe menyediakan dua jenis akun:

- **Pengguna:** Buat akun baru melalui halaman register, kemudian masuk menggunakan akun tersebut.
- **Admin:** Masuk menggunakan akun admin yang telah disediakan.

Pilih **Tanpa Login** untuk mencoba asesmen tanpa koneksi API. Hasil,
rekomendasi, dan checklist tamu hanya tersedia selama sesi dan tidak dikirim ke
server. Menu riwayat dan profil memerlukan login. Jika API edukasi tidak dapat
dihubungi, tamu dapat membaca materi bawaan yang mungkin belum terbaru; jika API
tersedia, halaman Edukasi menampilkan materi terpublikasi dari server.

Informasi akun admin:

- Email: [Isi email admin]
- Password: [Isi password admin]

## Cara Menggunakan Aplikasi

### Sebagai Pengguna

1. **Memulai Asesmen**

   Buka menu **Asesmen**, kemudian pilih jenis bencana yang ingin dinilai, yaitu Banjir, Cuaca Ekstrem, atau Suhu Panas Ekstrem.

2. **Menjawab Pertanyaan**

   Jawab lima pertanyaan yang tersedia. Setiap pertanyaan memiliki empat pilihan jawaban. Pilih jawaban yang paling sesuai dengan kondisi dan persiapanmu.

   Jika ingin mengubah jawaban sebelumnya, gunakan tombol **Kembali**.

3. **Melihat Hasil Asesmen**

   Setelah semua pertanyaan dijawab, klik **Lihat Hasil**. Aplikasi akan menampilkan skor total, tingkat kesiapan, dan skor dari setiap dimensi. Hasil asesmen juga akan tersimpan secara otomatis.

4. **Melihat Rekomendasi**

   Buka menu **Rekomendasi** untuk melihat tindakan yang disarankan berdasarkan hasil asesmen. Rekomendasi akan diurutkan berdasarkan tingkat prioritas.

5. **Menggunakan Checklist**

   Buka menu **Checklist** untuk melihat daftar tindakan yang perlu dilakukan. Beri tanda centang pada tindakan yang sudah diselesaikan. Aplikasi akan menampilkan persentase kemajuan secara otomatis.

6. **Melihat Riwayat Asesmen**

   Menu **Riwayat** menyimpan hasil asesmen yang pernah dilakukan. Melalui menu ini, pengguna dapat melihat dan membandingkan perkembangan tingkat kesiapan dari waktu ke waktu.

7. **Mempelajari Materi Edukasi**

   Buka menu **Edukasi** untuk membaca informasi dan materi seputar bencana yang tersedia di aplikasi.

### Cara Membaca Hasil Asesmen

Skor total digunakan untuk menentukan tingkat kesiapan pengguna. Berikut pembagian kategorinya:

| Skor Total | Tingkat Kesiapan |
|---|---|
| 80–100 | Siap |
| 40–79 | Cukup Siap |
| Di bawah 40 | Belum Siap |

Selain skor total, hasil asesmen juga menampilkan skor untuk setiap dimensi dalam bentuk grafik.

Setiap grafik memiliki garis target sebagai batas minimal yang perlu dicapai. Jika skor suatu dimensi masih di bawah garis tersebut, dimensi itu akan diberi keterangan **Perlu Ditingkatkan**.

Rekomendasi tindakan juga dibagi berdasarkan prioritas:

- **Prioritas Tinggi:** Untuk dimensi dengan skor di bawah 40.
- **Prioritas Sedang:** Untuk dimensi dengan skor 40 atau lebih.

### Sebagai Admin

Setelah login menggunakan akun admin, pengguna akan masuk ke halaman khusus admin. Menu navigasi tersedia di sebelah kiri.

Berikut fitur yang tersedia:

- **Kelola Pengguna:** Admin dapat mencari akun berdasarkan nama atau email, kemudian memilih akun yang ingin dikelola. Admin dapat mengubah nama pengguna, menonaktifkan atau mengaktifkan akun, serta menghapus akun. Tindakan menonaktifkan dan menghapus akun memerlukan konfirmasi terlebih dahulu. Admin juga tidak dapat menonaktifkan atau menghapus akun yang sedang digunakan untuk login.
- **Kelola Konten:** Admin dapat membuat beberapa materi untuk setiap bahaya, menyimpan draft, mempublikasikan, dan menarik publikasi. Pengguna melihat materi yang telah dipublikasikan.
- **Log & Monitoring:** Admin dapat memantau aktivitas aplikasi berdasarkan periode (hari ini, 7 hari terakhir, atau 30 hari terakhir) dan jenis bahaya. Halaman ini menampilkan jumlah seluruh pengguna, pengguna aktif, dan asesmen yang selesai. Tersedia juga tabel riwayat asesmen yang berisi waktu, nama pengguna, jenis bahaya, skor, dan tingkat kesiapan. Tombol Muat Ulang digunakan untuk memperbarui data.

Untuk keluar dari akun admin, klik tombol **Keluar** di bagian kiri bawah halaman.

## Alur Uji Cepat

Bagian ini dapat digunakan untuk memeriksa apakah fitur utama ClimateSafe sudah berjalan dengan baik.

1. Buat akun pengguna baru dan login.
2. Buka menu **Asesmen**, lalu pilih Banjir.
3. Jawab lima pertanyaan menggunakan kombinasi berikut. Angka menunjukkan urutan pilihan jawaban dari atas ke bawah, dengan pilihan pertama sebagai jawaban yang menunjukkan persiapan terbaik.

| Jawaban Pertanyaan 1–5 | Hasil yang Diharapkan |
|---|---|
| 1, 1, 1, 1, 1 | Siap |
| 3, 2, 1, 2, 2 | Cukup Siap |
| 4, 3, 4, 3, 4 | Belum Siap |

4. Setelah menyelesaikan asesmen, periksa menu **Rekomendasi**, **Checklist**, dan **Riwayat**.
5. Pastikan hasil asesmen yang baru dilakukan sudah muncul di menu Riwayat.

Jika ketiga skenario menghasilkan kategori yang sesuai, fitur utama asesmen sudah dapat diuji.

## Jika Terjadi Masalah

| Pesan atau Kondisi | Pemeriksaan |
|---|---|
| API tidak dapat dihubungi | Pastikan API berjalan dan `CLIMATESAFE_API_URL` menunjuk ke alamat yang benar. |
| API gagal mengakses database | Periksa PostgreSQL dan `ConnectionStrings__ClimateSafe` pada terminal API. |
| Tabel tidak ditemukan | Pastikan migrasi 000 sampai 007 telah dijalankan pada database yang digunakan API. |
| Login gagal | Periksa email, password, dan status akun. |

## Untuk Pengembang

Project desktop dan API memerlukan .NET 10 SDK. Petunjuk koneksi dan migrasi database tersedia di `ClimateSafe.Api/README.md`.

<img width="683" height="1154" alt="image" src="https://github.com/user-attachments/assets/5b9b892a-1113-48b4-88b7-7400b77ecbd3" />

<img width="1092" height="857" alt="ERD ClimateSafe Junpro" src="https://github.com/user-attachments/assets/17f1acf8-f5f0-4f52-9e26-2aad0d7b3ccd" />
