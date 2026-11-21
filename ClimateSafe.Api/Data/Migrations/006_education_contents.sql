BEGIN;

CREATE TABLE IF NOT EXISTS education_contents (
    content_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    hazard_id integer NOT NULL UNIQUE REFERENCES hazards(hazard_id),
    title text NOT NULL CHECK (length(trim(title)) > 0),
    body text NOT NULL CHECK (length(trim(body)) > 0),
    source_name text,
    source_url text,
    published_title text,
    published_body text,
    published_source_name text,
    published_source_url text,
    published_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by integer REFERENCES users(user_id) ON DELETE SET NULL,
    CHECK ((published_at IS NULL AND published_title IS NULL AND published_body IS NULL
            AND published_source_name IS NULL AND published_source_url IS NULL)
        OR (published_at IS NOT NULL AND published_title IS NOT NULL AND published_body IS NOT NULL))
);

COMMIT;
