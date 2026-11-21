-- =====================================================================
-- ClimateSafe - setup database (PostgreSQL)
-- Dibuat dari skema database yang dipakai aplikasi (dump pg_dump 18.6),
-- lalu dibersihkan agar bisa dijalankan di komputer mana pun:
--   * tanpa perintah OWNER TO (pemilik = pengguna yang menjalankan)
--   * tanpa data pengguna
--   * ditambah isian awal: hazards dan preparedness_dimensions
--
-- Cara pakai:
--   1. Otomatis : jalankan setup.bat (membuat database lalu menjalankan file ini)
--   2. Manual   : buat database kosong, buka file ini di pgAdmin (Query Tool)
--                 atau jalankan: psql -U postgres -d climatesafe -f setup.sql
--
-- File ini untuk DATABASE KOSONG. Jika database sudah berisi tabel
-- ClimateSafe, file berhenti dengan pesan error dan tidak mengubah apa pun.
-- =====================================================================

BEGIN;

DO $$
BEGIN
    IF to_regclass('public.users') IS NOT NULL
       OR to_regclass('public.assessments') IS NOT NULL
       OR to_regclass('public.hazards') IS NOT NULL THEN
        RAISE EXCEPTION 'Database sudah berisi tabel ClimateSafe. Gunakan database kosong atau jalankan: setup.bat reset';
    END IF;
END
$$;

-- ---------------------------------------------------------------------
-- Tabel
-- ---------------------------------------------------------------------

CREATE TABLE public.users (
    user_id       serial PRIMARY KEY,
    name          character varying(100),
    email         character varying(100) UNIQUE,
    password_hash character varying(255),
    role          character varying(20)  DEFAULT 'User'::character varying,
    status        character varying(20)  DEFAULT 'Active'::character varying,
    created_at    timestamp without time zone DEFAULT now()
);

CREATE TABLE public.hazards (
    hazard_id serial PRIMARY KEY,
    name      character varying(100)
);

CREATE TABLE public.preparedness_dimensions (
    dimension_id      serial PRIMARY KEY,
    name              character varying(100),
    passing_threshold numeric(5,2)
);

CREATE TABLE public.questions (
    question_id   integer PRIMARY KEY,
    hazard_id     integer NOT NULL REFERENCES public.hazards(hazard_id),
    dimension_id  integer NOT NULL REFERENCES public.preparedness_dimensions(dimension_id),
    question_text text NOT NULL,
    UNIQUE (hazard_id, dimension_id)
);

CREATE TABLE public.recommendations (
    recommendation_id integer PRIMARY KEY,
    hazard_id         integer NOT NULL REFERENCES public.hazards(hazard_id),
    dimension_id      integer NOT NULL REFERENCES public.preparedness_dimensions(dimension_id),
    description       text NOT NULL,
    priority          text NOT NULL,
    UNIQUE (hazard_id, dimension_id)
);

CREATE TABLE public.assessments (
    assessment_id  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    submission_key uuid NOT NULL UNIQUE,
    user_id        integer NOT NULL REFERENCES public.users(user_id) ON DELETE CASCADE,
    hazard_id      integer NOT NULL REFERENCES public.hazards(hazard_id),
    overall_score  double precision NOT NULL
                   CHECK (overall_score >= 0 AND overall_score <= 100),
    level          text NOT NULL,
    created_at     timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX assessments_user_created_idx
    ON public.assessments USING btree (user_id, created_at DESC);

CREATE TABLE public.assessment_answers (
    answer_id     integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assessment_id integer NOT NULL REFERENCES public.assessments(assessment_id) ON DELETE CASCADE,
    question_id   integer NOT NULL REFERENCES public.questions(question_id),
    value         text NOT NULL,
    score         integer NOT NULL CHECK (score >= 0 AND score <= 3),
    UNIQUE (assessment_id, question_id)
);

CREATE TABLE public.checklist_items (
    checklist_item_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assessment_id     integer NOT NULL REFERENCES public.assessments(assessment_id) ON DELETE CASCADE,
    recommendation_id integer NOT NULL REFERENCES public.recommendations(recommendation_id),
    description       text NOT NULL,
    is_completed      boolean NOT NULL DEFAULT false,
    completed_at      timestamp with time zone,
    UNIQUE (assessment_id, recommendation_id),
    CHECK ((is_completed AND completed_at IS NOT NULL)
        OR ((NOT is_completed) AND completed_at IS NULL))
);

-- ---------------------------------------------------------------------
-- Fungsi akun (st_*), dipakai UserRepository
-- ---------------------------------------------------------------------

CREATE FUNCTION public.st_register(_name character varying, _email character varying, _password_hash character varying) RETURNS integer
    LANGUAGE plpgsql
    AS $$
begin
    if exists (select 1 from users where email = _email) then
        return 0;  -- email sudah terdaftar
    end if;
    insert into users (name, email, password_hash) values (_name, _email, _password_hash);
    return 1;
end;
$$;

CREATE FUNCTION public.st_get_user(_email character varying) RETURNS TABLE(user_id integer, name character varying, email character varying, password_hash character varying, role character varying, status character varying, created_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
begin
    return query
    select u.user_id, u.name, u.email, u.password_hash, u.role, u.status, u.created_at
    from users u where u.email = _email;
end;
$$;

CREATE FUNCTION public.st_get_all_users() RETURNS TABLE(user_id integer, name character varying, email character varying, role character varying, status character varying, created_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
begin
    return query
    select u.user_id, u.name, u.email, u.role, u.status, u.created_at
    from users u order by u.user_id;
end;
$$;

CREATE FUNCTION public.st_update_profile(_user_id integer, _name character varying) RETURNS integer
    LANGUAGE plpgsql
    AS $$
begin
    update users set name = _name where user_id = _user_id;
    if found then return 1; else return 0; end if;
end;
$$;

CREATE FUNCTION public.st_set_status(_user_id integer, _status character varying) RETURNS integer
    LANGUAGE plpgsql
    AS $$
begin
    update users set status = _status where user_id = _user_id;
    if found then return 1; else return 0; end if;
end;
$$;

CREATE FUNCTION public.st_delete_user(_user_id integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
begin
    delete from checklist_items where assessment_id in
        (select assessment_id from assessments where user_id = _user_id);
    delete from assessment_answers where assessment_id in
        (select assessment_id from assessments where user_id = _user_id);
    delete from assessments where user_id = _user_id;
    delete from users where user_id = _user_id;
    if found then return 1; else return 0; end if;
end;
$$;

-- ---------------------------------------------------------------------
-- Isian awal (wajib ada agar Edukasi dan penyimpanan asesmen berjalan).
-- Urutan dan ejaan harus sama dengan yang dipakai aplikasi:
-- HazardId = urutan bahaya, dimension_id = urutan dimensi.
-- ---------------------------------------------------------------------

INSERT INTO public.hazards (hazard_id, name) VALUES
    (1, 'Banjir'),
    (2, 'Cuaca Ekstrem'),
    (3, 'Suhu Panas Ekstrem');

INSERT INTO public.preparedness_dimensions (dimension_id, name, passing_threshold) VALUES
    (1, 'Perlengkapan darurat', 75),
    (2, 'Jalur evakuasi', 75),
    (3, 'Kontak darurat', 75),
    (4, 'Rencana logistik', 75),
    (5, 'Pengetahuan bencana', 75);

-- Samakan penghitung urutan dengan isian di atas.
SELECT setval(pg_get_serial_sequence('public.hazards', 'hazard_id'), 3);
SELECT setval(pg_get_serial_sequence('public.preparedness_dimensions', 'dimension_id'), 5);

COMMIT;

-- ---------------------------------------------------------------------
-- Membuat akun Admin
-- Aplikasi menentukan admin dari kolom users.role = 'Admin'. Tidak ada akun
-- admin bawaan. Caranya:
--   1. Daftar akun biasa lewat aplikasi (layar Daftar).
--   2. Jalankan perintah berikut dengan email akun tersebut:
--
--      UPDATE public.users SET role = 'Admin' WHERE email = 'email@contoh.com';
--
--   3. Login ulang: akun itu kini membuka Panel Admin.
-- ---------------------------------------------------------------------
