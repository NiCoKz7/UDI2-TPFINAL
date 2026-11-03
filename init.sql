-- ============================================================
-- Esquema y datos de prueba (seeding) para la base local
-- Se ejecuta automáticamente la primera vez que se crea el
-- volumen de PostgreSQL (docker-entrypoint-initdb.d).
-- ============================================================

CREATE TABLE IF NOT EXISTS "Formatos" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS "Usuarios" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS "Perfumes" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(100) NOT NULL,
    "Marca" VARCHAR(100) NOT NULL,
    "Precio" NUMERIC(10, 2) NOT NULL,
    "UsuarioId" INTEGER NOT NULL,
    "FormatoId" INTEGER NOT NULL,
    CONSTRAINT "FK_Perfumes_Usuarios_UsuarioId"
        FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id"),
    CONSTRAINT "FK_Perfumes_Formatos_FormatoId"
        FOREIGN KEY ("FormatoId") REFERENCES "Formatos" ("Id")
);

-- Catálogo de formatos
INSERT INTO "Formatos" ("Nombre") VALUES
    ('Tubo'),
    ('Miniatura'),
    ('Botella')
ON CONFLICT ("Nombre") DO NOTHING;

-- Usuarios de prueba
INSERT INTO "Usuarios" ("Nombre", "Email") VALUES
    ('Nicolás', 'nico@example.com'),
    ('Ana', 'ana@example.com')
ON CONFLICT ("Email") DO NOTHING;

-- Perfumes de prueba
INSERT INTO "Perfumes" ("Nombre", "Marca", "Precio", "UsuarioId", "FormatoId") VALUES
    ('Sauvage', 'Dior', 85000.00, 1, 1),
    ('Bleu de Chanel', 'Chanel', 120000.00, 2, 2),
    ('La Vie Est Belle', 'Lancôme', 95000.00, 1, 3);
