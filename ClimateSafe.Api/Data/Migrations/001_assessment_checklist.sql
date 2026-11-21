BEGIN;

CREATE TABLE IF NOT EXISTS hazards (
    hazard_id integer PRIMARY KEY,
    name text NOT NULL
);
CREATE TABLE IF NOT EXISTS preparedness_dimensions (
    dimension_id integer PRIMARY KEY,
    name text NOT NULL,
    passing_threshold double precision NOT NULL CHECK (passing_threshold > 0 AND passing_threshold <= 100)
);
INSERT INTO hazards VALUES
    (1, 'Banjir'), (2, 'Cuaca Ekstrem'), (3, 'Suhu Panas Ekstrem')
ON CONFLICT (hazard_id) DO NOTHING;
INSERT INTO preparedness_dimensions VALUES
    (1, 'Perlengkapan darurat', 75), (2, 'Jalur evakuasi', 75),
    (3, 'Kontak darurat', 75), (4, 'Rencana logistik', 75), (5, 'Pengetahuan bencana', 75)
ON CONFLICT (dimension_id) DO NOTHING;

CREATE TABLE IF NOT EXISTS questions (
    question_id integer PRIMARY KEY,
    hazard_id integer NOT NULL REFERENCES hazards(hazard_id),
    dimension_id integer NOT NULL REFERENCES preparedness_dimensions(dimension_id),
    question_text text NOT NULL,
    UNIQUE (hazard_id, dimension_id)
);
CREATE TABLE IF NOT EXISTS recommendations (
    recommendation_id integer PRIMARY KEY,
    hazard_id integer NOT NULL REFERENCES hazards(hazard_id),
    dimension_id integer NOT NULL REFERENCES preparedness_dimensions(dimension_id),
    description text NOT NULL,
    priority text NOT NULL,
    UNIQUE (hazard_id, dimension_id)
);
CREATE TABLE IF NOT EXISTS assessments (
    assessment_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    submission_key uuid NOT NULL UNIQUE,
    user_id integer NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    hazard_id integer NOT NULL REFERENCES hazards(hazard_id),
    overall_score double precision NOT NULL CHECK (overall_score BETWEEN 0 AND 100),
    level text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS assessments_user_created_idx ON assessments(user_id, created_at DESC);
CREATE TABLE IF NOT EXISTS assessment_answers (
    answer_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assessment_id integer NOT NULL REFERENCES assessments(assessment_id) ON DELETE CASCADE,
    question_id integer NOT NULL REFERENCES questions(question_id),
    value text NOT NULL,
    score integer NOT NULL CHECK (score BETWEEN 0 AND 3),
    UNIQUE (assessment_id, question_id)
);
CREATE TABLE IF NOT EXISTS checklist_items (
    checklist_item_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assessment_id integer NOT NULL REFERENCES assessments(assessment_id) ON DELETE CASCADE,
    recommendation_id integer NOT NULL REFERENCES recommendations(recommendation_id),
    description text NOT NULL,
    is_completed boolean NOT NULL DEFAULT false,
    completed_at timestamptz,
    UNIQUE (assessment_id, recommendation_id),
    CHECK ((is_completed AND completed_at IS NOT NULL) OR
           (NOT is_completed AND completed_at IS NULL))
);

COMMIT;
