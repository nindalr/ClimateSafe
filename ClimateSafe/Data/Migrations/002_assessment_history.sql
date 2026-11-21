BEGIN;

-- Jawaban baru menyimpan teks saat asesmen. Data lama memakai master yang tersedia.
ALTER TABLE assessment_answers ADD COLUMN IF NOT EXISTS question_text text;
ALTER TABLE assessment_answers ADD COLUMN IF NOT EXISTS dimension_name text;

UPDATE assessment_answers aa
SET question_text = COALESCE(aa.question_text, q.question_text),
    dimension_name = COALESCE(aa.dimension_name, d.name)
FROM questions q
JOIN preparedness_dimensions d ON d.dimension_id = q.dimension_id
WHERE aa.question_id = q.question_id
  AND (aa.question_text IS NULL OR aa.dimension_name IS NULL);

COMMIT;
