# ClimateSafe

## Nama Kelompok
Kelompok 17

## Anggota dan NIM Kelompok
- **Ketua Kelompok:** Nathanael Satya Saputra 
- **Anggota 1: Nathanael Satya Saputra** – 24/534424/TK/59236 (Backend Developer)
- **Anggota 2: Zahra Elfatima** – 24/535709/TK/59448 (Frontend Developer)
- **Anggota 3: Ninda Alifa Rachmayanti** – 24/545484/TK/60679 (Software Architect)

## Project Senior Project TI

**Instansi:** Departemen Teknologi Elektro dan Teknologi Informasi, Fakultas Teknik, Universitas Gadjah Mada

---

## Jawaban Modul 1

### Nama Produk
ClimateSafe

### Jenis Produk
Kategori: Lingkungan, Kesiapsiagaan dan Mitigasi Bencana

Tipe Aplikasi: Desktop

### Latar Belakang & Permasalahan
Perubahan iklim meningkatkan berbagai risiko yang berkaitan dengan cuaca dan bahaya terkait iklim, sehingga peningkatan pemahaman dan kesiapsiagaan masyarakat menjadi hal penting. Masyarakat tidak hanya membutuhkan informasi mengenai bahaya, tetapi juga panduan mengenai tindakan kesiapsiagaan yang dapat dilakukan.

BNPB melalui Buku Pedoman Kesiapsiagaan untuk Keluarga menekankan pentingnya mengetahui ancaman dan risiko di sekitar, memperhatikan langkah mitigasi praktis, serta menyiapkan rencana kesiapsiagaan keluarga. Namun, informasi kesiapsiagaan berasal dari berbagai sumber yang tersebar, sehingga pengguna umum sering kesulitan mengubah informasi tersebut menjadi langkah yang terstruktur dan dapat dilakukan secara bertahap.

Permasalahan ini berkaitan dengan kebutuhan untuk meningkatkan ketahanan, kapasitas adaptasi, edukasi, dan kesadaran masyarakat terhadap bahaya terkait iklim, sejalan dengan **SDG 13: Climate Action**, khususnya Target 13.1 (penguatan ketahanan dan kapasitas adaptasi terhadap bahaya iklim dan bencana alam) dan Target 13.3 (peningkatan pendidikan, kesadaran, dan kapasitas terkait perubahan iklim).

### Ide Solusi
ClimateSafe diusulkan sebagai aplikasi desktop yang membantu individu mengevaluasi kesiapsiagaan terhadap bahaya terkait iklim melalui **hazard-specific preparedness assessment**, mulai dari assessment, pemahaman hasil, rekomendasi tindakan, hingga pemantauan progres kesiapsiagaan.

ClimateSafe **bukan** sistem prediksi bencana dan **bukan** sistem peringatan dini resmi, informasi resmi tetap mengacu pada lembaga berwenang (BNPB, BPBD, BMKG).

**Fitur Utama:**
1. **Hazard Selection**: pengguna memilih jenis bahaya yang ingin dinilai (MVP: Flood dan Extreme Heat).
2. **Hazard-Specific Preparedness Assessment**: pertanyaan diturunkan dari sumber relevan (BNPB untuk Flood; WHO untuk Extreme Heat).
3. **Preparedness Scoring**: jawaban diproses menjadi skor 0–100 per aspek (skala: Sudah/Ya = 2, Sebagian = 1, Belum = 0), lalu dirata-rata menjadi overall preparedness score.
4. **Preparedness Gap Identification**: sistem mengidentifikasi aspek kesiapsiagaan yang masih perlu diperbaiki.
5. **Personalized Recommendation**: rekomendasi tindakan berdasarkan gap yang teridentifikasi.
6. **Preparedness Checklist**: rekomendasi diubah menjadi checklist tindakan yang dapat dipantau.
7. **Progress Tracking**: pengguna menandai checklist selesai/belum, ditampilkan sebagai persentase progres.
8. **Assessment History**: hasil assessment disimpan di database sehingga pengguna dapat melihat riwayat dan perubahannya dari waktu ke waktu.

**Alur penyusunan assessment:** Sumber Resmi/Literatur → Preparedness Dimension → Indicator → Assessment Question → Answer Option → Score → Preparedness Gap → Recommendation → Checklist

**Batasan Aplikasi:**
- Tidak memprediksi kapan bencana terjadi
- Tidak menentukan probabilitas seseorang terkena bencana
- Tidak menggantikan sistem peringatan dini resmi maupun informasi dari BNPB/BPBD/BMKG
- Preparedness Score hanya menggambarkan kondisi berdasarkan indikator assessment aplikasi
- Rekomendasi bersifat umum, bukan keputusan profesional untuk kondisi darurat tertentu

### Analisis Kompetitor

| Aplikasi | Fokus | Perbedaan dengan ClimateSafe |
|---|---|---|
| **InaRISK** (BNPB) | Informasi risiko bencana & pemetaan wilayah | ClimateSafe tidak berfokus pada pemetaan risiko wilayah, melainkan pada assessment kesiapsiagaan individu → score → gap → rekomendasi → checklist → progress |
| **FEMA App** | Informasi & layanan kesiapsiagaan darurat resmi (AS) | ClimateSafe tidak menggantikan layanan resmi pemerintah; fokus lebih sempit pada self-assessment kesiapsiagaan individu dan pemantauan tindakan |
| **Ready.gov** | Informasi & panduan kesiapsiagaan terhadap keadaan darurat | Ready.gov berfungsi sebagai sumber informasi/panduan, sedangkan ClimateSafe mengubah prinsip preparedness menjadi proses assessment yang menghasilkan score dan rekomendasi yang dapat ditindaklanjuti |

**Pembeda Utama ClimateSafe:** bukan pada ketersediaan informasi bencana baru, melainkan pada **alur pengolahan informasi menjadi tindakan personal**, pengguna dapat mengevaluasi kesiapsiagaannya, mengetahui aspek yang masih kurang, memperoleh rekomendasi, melakukan tindakan, dan memantau progresnya.
