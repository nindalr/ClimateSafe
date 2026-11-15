# ClimateSafe

### Personal Climate Disaster Preparedness Assessment & Action Planner

**Kelompok 17**

- **Ketua Kelompok:** Nathanael Satya Saputra - 24/534424/TK/59236
- **Anggota 1:** Nathanael Satya Saputra - 24/534424/TK/59236
- **Anggota 2:** Zahra Elfatima - 24/535709/TK/59448
- **Anggota 3:** Ninda Alifa Rachmayanti - 24/545484/TK/60679

ClimateSafe is a desktop-based application that helps users assess their
preparedness for selected climate-related hazards, identify areas for
improvement, and take appropriate preparedness actions.

## Konfigurasi database lokal

Default PostgreSQL adalah `localhost:5432`, user `postgres`, dan database
`climatesafe`. Tabel dan fungsi SQL akun dari HW8 harus tersedia di database tersebut.

1. Salin `.env.example` menjadi `.env` di root repository jika `.env` belum ada.
2. Sesuaikan `CLIMATESAFE_DB_HOST`, `CLIMATESAFE_DB_PORT`,
   `CLIMATESAFE_DB_USERNAME`, `CLIMATESAFE_DB_NAME`, dan `CLIMATESAFE_DB_PASSWORD`
   pada `.env`. Gunakan kutip tunggal untuk password agar `$` dan `#` tetap literal.
3. Jalankan `dotnet run --project ClimateSafe/ClimateSafe.csproj`.

File `.env` diabaikan Git. Saat build, file ini disalin ke folder executable agar
aplikasi dapat membacanya tanpa bergantung pada working directory. Setelah mengedit
`.env` di root, build ulang dan buka ulang aplikasi. File `.env.example` boleh
di-commit; `.env` yang berisi password tidak boleh di-commit.

Environment variable dengan nama yang sama dapat digunakan sebagai alternatif
dan memiliki prioritas lebih tinggi daripada `.env`. Jika host, port, username,
atau nama database tidak diberikan, nilai default di atas digunakan. Password wajib
diisi. Publish tidak menyertakan
`.env`; siapkan file tersebut di sebelah executable atau gunakan environment
variable pada komputer tujuan. `.env` adalah file teks biasa, bukan enkripsi.

<img width="683" height="1154" alt="image" src="https://github.com/user-attachments/assets/5b9b892a-1113-48b4-88b7-7400b77ecbd3" />

<img width="1092" height="857" alt="ERD ClimateSafe Junpro" src="https://github.com/user-attachments/assets/17f1acf8-f5f0-4f52-9e26-2aad0d7b3ccd" />

