-- ============================================================
-- Esquema y datos de prueba (seeding).
--
-- Las tablas usan el prefijo "udi2_" para convivir sin choques en
-- la base compartida del VPS (una sola base por alumno).
--
-- En local se ejecuta automáticamente al crear el volumen de
-- PostgreSQL (docker-entrypoint-initdb.d).
-- En el VPS se ejecuta una vez a mano contra la base asignada.
-- ============================================================

CREATE TABLE IF NOT EXISTS "udi2_formatos" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS "udi2_usuarios" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS "udi2_perfumes" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(100) NOT NULL,
    "Marca" VARCHAR(100) NOT NULL,
    "Precio" NUMERIC(10, 2) NOT NULL,
    "UsuarioId" INTEGER NOT NULL,
    "FormatoId" INTEGER NOT NULL,
    CONSTRAINT "FK_udi2_perfumes_udi2_usuarios_UsuarioId"
        FOREIGN KEY ("UsuarioId") REFERENCES "udi2_usuarios" ("Id"),
    CONSTRAINT "FK_udi2_perfumes_udi2_formatos_FormatoId"
        FOREIGN KEY ("FormatoId") REFERENCES "udi2_formatos" ("Id")
);

-- Catálogo de formatos
INSERT INTO "udi2_formatos" ("Nombre") VALUES
    ('Tubo'),
    ('Miniatura'),
    ('Botella')
ON CONFLICT ("Nombre") DO NOTHING;

-- Usuarios de prueba
INSERT INTO "udi2_usuarios" ("Nombre", "Email") VALUES
    ('Nicolás', 'nico@example.com'),
    ('Ana', 'ana@example.com')
ON CONFLICT ("Email") DO NOTHING;

-- Perfumes de prueba (solo si la tabla está vacía)
INSERT INTO "udi2_perfumes" ("Nombre", "Marca", "Precio", "UsuarioId", "FormatoId")
SELECT * FROM (VALUES
    ('Sauvage', 'Dior', 85000.00, 1, 1),
    ('Bleu de Chanel', 'Chanel', 120000.00, 2, 2),
    ('La Vie Est Belle', 'Lancôme', 95000.00, 1, 3)
) AS v("Nombre", "Marca", "Precio", "UsuarioId", "FormatoId")
WHERE NOT EXISTS (SELECT 1 FROM "udi2_perfumes");
