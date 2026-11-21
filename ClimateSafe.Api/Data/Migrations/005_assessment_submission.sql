BEGIN;

CREATE TABLE IF NOT EXISTS recommendation_templates (
    hazard_id integer NOT NULL REFERENCES hazards(hazard_id),
    dimension_id integer NOT NULL REFERENCES preparedness_dimensions(dimension_id),
    description text NOT NULL,
    PRIMARY KEY (hazard_id, dimension_id)
);

INSERT INTO recommendation_templates (hazard_id, dimension_id, description) VALUES
    (1, 1, 'Siapkan tas darurat dan simpan salinan dokumen penting dalam wadah kedap air.'),
    (1, 2, 'Tentukan jalur evakuasi menuju tempat lebih tinggi dan hindari rute yang melewati genangan.'),
    (1, 3, 'Catat kontak BPBD, petugas setempat, dan keluarga yang dapat dihubungi saat banjir.'),
    (1, 4, 'Rencanakan persediaan kebutuhan keluarga di tempat yang terlindung dari air banjir.'),
    (1, 5, 'Pelajari peringatan banjir serta prosedur evakuasi dari petugas setempat.'),
    (2, 1, 'Siapkan senter, baterai cadangan, dan tas darurat untuk menghadapi gangguan listrik saat badai.'),
    (2, 2, 'Tentukan bangunan kokoh sebagai tempat berlindung dan rute yang menghindari pohon serta papan reklame.'),
    (2, 3, 'Simpan kontak petugas darurat, penyedia listrik, dan keluarga untuk situasi cuaca ekstrem.'),
    (2, 4, 'Rencanakan kebutuhan keluarga jika cuaca ekstrem menghambat perjalanan atau distribusi barang.'),
    (2, 5, 'Pelajari peringatan cuaca BMKG dan prosedur berlindung saat angin kencang atau petir.'),
    (3, 1, 'Siapkan wadah air minum dan perlengkapan pelindung dari sinar matahari saat bepergian.'),
    (3, 2, 'Tentukan tempat sejuk atau teduh yang dapat dijangkau saat suhu panas ekstrem.'),
    (3, 3, 'Catat kontak keluarga dan layanan darurat yang dapat dihubungi saat membutuhkan bantuan.'),
    (3, 4, 'Rencanakan ketersediaan air minum dan kebutuhan anggota keluarga selama periode panas ekstrem.'),
    (3, 5, 'Pelajari informasi peringatan suhu panas serta panduan kesiapsiagaan dari instansi terkait.')
ON CONFLICT (hazard_id, dimension_id) DO NOTHING;

-- Pilihan yang dikirim disimpan agar pengiriman ulang dapat dibandingkan tanpa
-- bergantung pada teks atau bobot katalog yang mungkin berubah kemudian.
CREATE TABLE IF NOT EXISTS assessment_submission_options (
    assessment_id integer NOT NULL REFERENCES assessments(assessment_id) ON DELETE CASCADE,
    question_id integer NOT NULL REFERENCES questions(question_id),
    option_id integer NOT NULL REFERENCES question_options(option_id),
    PRIMARY KEY (assessment_id, question_id)
);

COMMIT;
