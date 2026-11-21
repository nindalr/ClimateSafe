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

## Menjalankan aplikasi dan API

Siapkan PostgreSQL, jalankan migrasi 000 sampai 005, dan jalankan API sesuai
petunjuk di `ClimateSafe.Api/README.md`. Pada terminal lain, jalankan WinForms:

```powershell
$env:CLIMATESAFE_API_URL = 'http://localhost:5028'
dotnet run --project ClimateSafe/ClimateSafe.csproj
```

Jika `CLIMATESAFE_API_URL` tidak diatur, aplikasi menggunakan alamat localhost
tersebut. Untuk server selain localhost, gunakan alamat HTTPS. Konfigurasi dan
password PostgreSQL hanya diperlukan oleh API.

<img width="683" height="1154" alt="image" src="https://github.com/user-attachments/assets/5b9b892a-1113-48b4-88b7-7400b77ecbd3" />

<img width="1092" height="857" alt="ERD ClimateSafe Junpro" src="https://github.com/user-attachments/assets/17f1acf8-f5f0-4f52-9e26-2aad0d7b3ccd" />

