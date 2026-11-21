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

### Langkah 1. Menyiapkan Database

Pastikan PostgreSQL sudah terinstal dan berjalan di komputer. Aplikasi ini dibuat dan diuji menggunakan PostgreSQL 18.

1. Buka folder `database` yang tersedia dalam paket aplikasi.
2. Klik dua kali file `setup.bat`.
3. Jika diminta memasukkan password, ketik password PostgreSQL untuk pengguna `postgres`, lalu tekan Enter.
4. Tunggu hingga proses selesai tanpa muncul pesan error.

Setelah berhasil, database dengan nama `climatesafe` akan dibuat dan sudah dilengkapi dengan tabel serta data awal yang dibutuhkan aplikasi.

**Catatan:** Jika ingin mengulang proses dari awal, buka Command Prompt pada folder `database`, lalu jalankan perintah `setup.bat reset`. Perintah ini akan menghapus data lama dan membuat ulang database. Pastikan tidak ada data penting yang masih dibutuhkan.

### Langkah 2. Mengatur Koneksi Database

Aplikasi membutuhkan file `.env` untuk terhubung dengan database PostgreSQL.

1. Buka folder aplikasi yang berisi file `ClimateSafe.exe`.
2. Cari file `.env.example`, lalu buat salinannya.
3. Ubah nama file salinan tersebut menjadi `.env`.
4. Buka file `.env` menggunakan Notepad, kemudian isi pengaturannya seperti berikut:

```env
CLIMATESAFE_DB_HOST='localhost'
CLIMATESAFE_DB_PORT='5432'
CLIMATESAFE_DB_USERNAME='postgres'
CLIMATESAFE_DB_NAME='climatesafe'
CLIMATESAFE_DB_PASSWORD='password-postgres-kamu'
```

5. Ganti `password-postgres-kamu` dengan password PostgreSQL milik pengguna `postgres` di komputer tersebut. Tanda kutip tunggal harus tetap digunakan.
6. Jika pengaturan host, port, atau nama pengguna PostgreSQL berbeda, sesuaikan nilainya dengan konfigurasi yang digunakan.
7. Simpan file `.env`.

Pastikan file `.env` berada dalam satu folder dengan `ClimateSafe.exe`.

### Langkah 3. Menjalankan Aplikasi

Klik dua kali file `ClimateSafe.exe` untuk membuka aplikasi. Jika berhasil, halaman login akan muncul.

Jika aplikasi gagal dibuka atau tidak dapat terhubung ke database, periksa pesan error yang muncul. Penjelasan lebih lanjut tersedia pada bagian **Jika Terjadi Masalah**.

### Langkah 4. Login ke Aplikasi

ClimateSafe menyediakan dua jenis akun:

- **Pengguna:** Buat akun baru melalui halaman register, kemudian masuk menggunakan akun tersebut.
- **Admin:** Masuk menggunakan akun admin yang telah disediakan.

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
- **Kelola Konten:** Admin dapat mengubah ringkasan materi edukasi untuk setiap jenis bahaya (Banjir, Cuaca Ekstrem, dan Suhu Panas Ekstrem). Admin dapat mengedit, menyimpan, atau membatalkan perubahan. Konten yang sudah disimpan akan ditampilkan pada menu Edukasi pengguna. Jika berpindah ke bahaya lain sebelum menyimpan perubahan, aplikasi akan meminta konfirmasi.
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

Jika terjadi kesalahan, aplikasi akan menampilkan pesan singkat beserta bagian **Detail teknis** yang menjelaskan penyebabnya.

Gunakan tabel berikut untuk mengetahui penyebab dan cara mengatasinya.

| Pesan atau Kondisi | Penyebab dan Cara Mengatasi |
|---|---|
| Gagal terhubung ke database | PostgreSQL mungkin belum berjalan atau pengaturan dalam file `.env` belum benar. Periksa host, port, nama database, dan password. Setelah diperbaiki, tutup lalu buka kembali aplikasi. |
| Password authentication failed | Password pada file `.env` tidak sesuai dengan password PostgreSQL. Periksa dan perbaiki password, kemudian buka kembali aplikasi. |
| Muncul pesan "column ... does not exist" atau "relation ... does not exist" | Database belum disiapkan atau masih menggunakan struktur tabel yang lama. Jalankan kembali `setup.bat reset` sesuai petunjuk pada Langkah 1. |
| Aplikasi tidak dapat dibuka | Pastikan file `.env` berada dalam satu folder dengan `ClimateSafe.exe`. Periksa juga apakah nama file benar-benar `.env`, bukan `.env.txt`. |
| Tidak bisa login meskipun data akun sudah benar | Akun mungkin sedang dinonaktifkan oleh admin. Hubungi admin untuk memeriksa status akun. |

## Untuk Pengembang: Menjalankan Aplikasi dari Kode Sumber

Jika ingin menjalankan ClimateSafe melalui kode sumber, pastikan .NET 10 SDK dan PostgreSQL sudah terinstal.

Setelah database berhasil dibuat dan file `.env` sudah diatur, letakkan file `.env` di folder utama repository (root repository).

Selanjutnya, buka terminal pada folder utama repository dan jalankan perintah berikut:

```bash
dotnet run --project ClimateSafe/ClimateSafe.csproj
```

Jika ingin membangun ulang aplikasi, pastikan ClimateSafe sudah ditutup terlebih dahulu. File aplikasi yang masih berjalan tidak dapat ditimpa selama proses pembangunan ulang.


<img width="683" height="1154" alt="image" src="https://github.com/user-attachments/assets/5b9b892a-1113-48b4-88b7-7400b77ecbd3" />

<img width="1092" height="857" alt="ERD ClimateSafe Junpro" src="https://github.com/user-attachments/assets/17f1acf8-f5f0-4f52-9e26-2aad0d7b3ccd" />
