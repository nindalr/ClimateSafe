@echo off
setlocal EnableExtensions
cd /d "%~dp0"

rem ======================================================================
rem  ClimateSafe - setup database
rem  Membuat database dan menjalankan setup.sql (satu folder dengan file ini).
rem
rem  Pemakaian:
rem    setup.bat          buat database jika belum ada, lalu isi dengan setup.sql
rem    setup.bat reset    HAPUS database lama beserta isinya, lalu buat ulang
rem
rem  Password PostgreSQL ditanyakan oleh psql dan tidak disimpan di file ini.
rem ======================================================================

rem ---- Pengaturan (ubah bila perlu, harus sama dengan isi .env) ----
set "DB_HOST=localhost"
set "DB_PORT=5432"
set "DB_USER=postgres"
set "DB_NAME=climatesafe"

echo ==================================================
echo  ClimateSafe - Setup database
echo ==================================================
echo  Host     : %DB_HOST%
echo  Port     : %DB_PORT%
echo  Pengguna : %DB_USER%
echo  Database : %DB_NAME%
echo.

if not exist "setup.sql" (
    echo [GAGAL] File setup.sql tidak ditemukan di folder ini.
    pause
    exit /b 1
)

rem ---- Cari psql ----
set "PSQL="
where psql >nul 2>nul
if not errorlevel 1 set "PSQL=psql"

if not defined PSQL (
    for /f "tokens=3*" %%A in ('reg query "HKLM\SOFTWARE\PostgreSQL\Installations" /s /v "Base Directory" 2^>nul ^| find "REG_SZ"') do (
        if not defined PSQL if exist "%%B\bin\psql.exe" set "PSQL=%%B\bin\psql.exe"
    )
)
if not defined PSQL (
    for /f "tokens=3*" %%A in ('reg query "HKLM\SOFTWARE\WOW6432Node\PostgreSQL\Installations" /s /v "Base Directory" 2^>nul ^| find "REG_SZ"') do (
        if not defined PSQL if exist "%%B\bin\psql.exe" set "PSQL=%%B\bin\psql.exe"
    )
)
if not defined PSQL (
    for /f "delims=" %%P in ('dir /b /ad /o-n "%ProgramFiles%\PostgreSQL" 2^>nul') do (
        if not defined PSQL if exist "%ProgramFiles%\PostgreSQL\%%P\bin\psql.exe" set "PSQL=%ProgramFiles%\PostgreSQL\%%P\bin\psql.exe"
    )
)

if defined PSQL goto :psql_siap
echo psql tidak ditemukan otomatis.
set /p "PGBIN=Ketik folder bin PostgreSQL, contoh C:\Program Files\PostgreSQL\18\bin : "
if exist "%PGBIN%\psql.exe" set "PSQL=%PGBIN%\psql.exe"

:psql_siap
if not defined PSQL (
    echo [GAGAL] psql.exe tidak ditemukan. Pasang PostgreSQL terlebih dahulu.
    pause
    exit /b 1
)

rem ---- Konfirmasi untuk reset ----
if /i not "%~1"=="reset" goto :buat_skrip
echo PERINGATAN: database %DB_NAME% akan DIHAPUS beserta seluruh datanya.
set /p "KONFIRMASI=Ketik YES lalu Enter untuk melanjutkan: "
if /i not "%KONFIRMASI%"=="YES" (
    echo Dibatalkan. Tidak ada yang diubah.
    pause
    exit /b 1
)

:buat_skrip
rem ---- Skrip bantu: dibuat sementara agar password hanya ditanyakan satu kali ----
set "DRV=%TEMP%\climatesafe_setup_driver.sql"
> "%DRV%" echo \set ON_ERROR_STOP on
if /i "%~1"=="reset" >> "%DRV%" echo DROP DATABASE IF EXISTS %DB_NAME% WITH (FORCE);
>> "%DRV%" echo SELECT 'CREATE DATABASE %DB_NAME%' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '%DB_NAME%')\gexec
>> "%DRV%" echo \connect %DB_NAME%
>> "%DRV%" echo \i setup.sql

set "PGCLIENTENCODING=UTF8"
echo Menjalankan setup. Ketik password PostgreSQL saat diminta
echo (huruf tidak tampil saat mengetik, ini normal).
echo.

"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d postgres -v ON_ERROR_STOP=1 -f "%DRV%"
set "HASIL=%ERRORLEVEL%"
del "%DRV%" >nul 2>nul

if not "%HASIL%"=="0" (
    echo.
    echo [GAGAL] Setup tidak selesai. Baca pesan error di atas.
    echo  - Password salah atau PostgreSQL belum berjalan: periksa layanan PostgreSQL.
    echo  - Database sudah berisi tabel: jalankan  setup.bat reset
    pause
    exit /b 1
)

echo.
echo ==================================================
echo  [BERHASIL] Database %DB_NAME% siap dipakai.
echo ==================================================
echo  Langkah berikutnya:
echo   1. Salin .env.example menjadi .env di root repository.
echo   2. Isi CLIMATESAFE_DB_HOST, _PORT, _USERNAME, _NAME, dan _PASSWORD
echo      (nilai harus sama dengan pengaturan di atas; password pakai kutip tunggal).
echo   3. Build dan jalankan aplikasi.
echo.
pause
endlocal
