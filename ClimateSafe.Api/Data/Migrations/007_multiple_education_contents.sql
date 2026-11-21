BEGIN;

ALTER TABLE education_contents DROP CONSTRAINT IF EXISTS education_contents_hazard_id_key;
CREATE INDEX IF NOT EXISTS ix_education_contents_hazard_id ON education_contents(hazard_id);

COMMIT;
