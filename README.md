# Perfumes API — TP Final UDI 2

API REST desarrollada con **.NET 10 (Minimal APIs)** y **PostgreSQL 17**, contenerizada con Docker y preparada para desplegarse en un VPS Linux.

Trabajo Práctico Final de **UDI 2: Infraestructura para Aplicaciones Backend** — ISP N° 20.

## Stack

- .NET 10 / ASP.NET Core Minimal APIs
- Entity Framework Core + Npgsql
- PostgreSQL 17 en contenedor local
- Docker multi-stage
- Docker Compose (con override para desarrollo local)
- Kestrel en dos puertos: **5000** (público) y **8080** (administración/diagnóstico)

## Modelo de datos

Tres tablas relacionadas:

| Tabla | Nombre físico | Campos |
|---|---|---|
| **Usuarios** | `udi2_usuarios` | `Id`, `Nombre`, `Email` |
| **Formatos** | `udi2_formatos` | `Id`, `Nombre` (`Tubo`, `Miniatura`, `Botella`) |
| **Perfumes** | `udi2_perfumes` | `Id`, `Nombre`, `Marca`, `Precio`, `UsuarioId` (FK), `FormatoId` (FK) |

> El prefijo `udi2_` permite convivir en la **base compartida del VPS** (una sola base por alumno) sin chocar con tablas de otros proyectos o materias. En la API los nombres siguen siendo `usuarios`, `formatos` y `perfumes`: el prefijo es solo el nombre físico de la tabla.

Relaciones:

- Un **usuario** puede tener muchos **perfumes**.
- Un **formato** puede estar asociado a muchos **perfumes**.

El esquema y los datos de prueba se crean con `init.sql`:

- **En local:** se ejecuta automáticamente la primera vez que se inicializa el volumen de PostgreSQL (`docker-entrypoint-initdb.d`).
- **En el VPS:** se ejecuta una vez a mano contra la base asignada (ver pasos de despliegue).

## Endpoints

### Obligatorios del TP (6)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/usuarios` | Lista los usuarios |
| GET | `/api/formatos` | Lista los formatos |
| GET | `/api/perfumes` | Lista perfumes; acepta `?usuarioId=` y `?formatoId=` |
| POST | `/api/perfumes` | Crea un perfume |
| PUT | `/api/perfumes/{id}` | Actualiza un perfume |
| DELETE | `/api/perfumes/{id}` | Elimina un perfume |

### Diagnóstico (adicionales)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/status` | Nombre de la app, entorno, flag de logs y `DATABASE_TIMEOUT` |
| GET | `/api/kestrel-info` | Métricas de ThreadPool, memoria y procesadores (solo puerto 8080) |

### Ejemplos

```bash
curl http://localhost:5007/api/status
curl http://localhost:5007/api/usuarios
curl http://localhost:5007/api/formatos
curl http://localhost:5007/api/perfumes
curl "http://localhost:5007/api/perfumes?formatoId=1"

curl -X POST http://localhost:5007/api/perfumes \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Sauvage","marca":"Dior","precio":85000,"usuarioId":1,"formatoId":1}'

curl -X PUT http://localhost:5007/api/perfumes/1 \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Sauvage Elixir","marca":"Dior","precio":99000,"usuarioId":1,"formatoId":1}'

curl -X DELETE http://localhost:5007/api/perfumes/1

curl http://localhost:8087/api/kestrel-info
```

En PowerShell:

```powershell
Invoke-RestMethod -Uri "http://localhost:5007/api/perfumes"
Invoke-RestMethod -Uri "http://localhost:5007/api/perfumes" -Method Post `
  -Body '{"nombre":"Sauvage","marca":"Dior","precio":85000,"usuarioId":1,"formatoId":1}' `
  -ContentType "application/json"
```

## Requisitos

- Docker Desktop (Windows/Mac) o Docker Engine + Compose (Linux)
- Git
- .NET 10 SDK (opcional, solo para desarrollo sin Docker)

## Levantar en local

1. Crear el archivo `.env` a partir del ejemplo:

   ```bash
   cp .env.example .env
   ```

   En PowerShell:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Editar `.env` y definir al menos `POSTGRES_PASSWORD`.

3. Levantar el stack con el override local:

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
   ```

   > La primera ejecución descarga las imágenes base de .NET y PostgreSQL, así que puede tardar varios minutos.

4. Verificar:

   ```bash
   docker compose ps
   docker logs -f perfumes_api
   curl http://localhost:5007/api/status
   ```

5. Para borrar los datos y volver a ejecutar el seeding:

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.local.yml down -v
   ```

La API queda en `http://localhost:5007` y el puerto administrativo en `http://localhost:8087`.

Con el override local, PostgreSQL también queda accesible desde el host en `localhost:5433` (se usa 5433 para no chocar con un PostgreSQL instalado en el host que ya ocupe 5432).

## Despliegue en el VPS (examen)

> En el examen el profesor asigna el usuario, la IP y los puertos. Hasta ese momento no hace falta tener acceso al VPS.

1. Conectarse por SSH:

   ```bash
   ssh usuario@IP_DEL_VPS -p PUERTO
   ```

2. Instalar Docker, Compose y Git (si no están):

   ```bash
   sudo apt update
   sudo apt install -y docker.io docker-compose-plugin git
   sudo systemctl enable --now docker
   sudo usermod -aG docker $USER
   ```

   Cerrar y volver a entrar a la sesión SSH para aplicar el grupo.

3. Clonar el repositorio:

   ```bash
   git clone https://github.com/TU_USUARIO/TU_REPO.git
   cd TU_REPO
   ```

4. Crear y editar el `.env`:

   ```bash
   cp .env.example .env
   nano .env
   ```

   Ajustar con los datos que entrega el profesor:

   - `API_PORT` y `ADMIN_PORT` con los puertos asignados (ej. `5007` y `8087`).
   - `DB_HOST`, `DB_INTERNAL_PORT`, `POSTGRES_DB`, `POSTGRES_USER` y `POSTGRES_PASSWORD` con los datos del **PostgreSQL compartido**.
   - `ASPNETCORE_ENVIRONMENT=Production`.
   - `ENABLE_DETAILED_LOGS=false`.

   > Si el PostgreSQL compartido escucha en el host del VPS, usar `DB_HOST=host.docker.internal` (el compose ya agrega `host-gateway`).

5. Crear las tablas y cargar los datos de prueba en la base asignada (una sola vez):

   ```bash
   set -a
   source .env
   set +a

   docker run --rm -v "$PWD/init.sql:/init.sql:ro" \
     --add-host=host.docker.internal:host-gateway \
     -e PGPASSWORD="$POSTGRES_PASSWORD" \
     postgres:17-alpine \
     psql -h "$DB_HOST" -p "$DB_INTERNAL_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f /init.sql
   ```

   > La API también crea el esquema automáticamente al arrancar (`EnsureCreated`) cuando la base está vacía, pero `init.sql` es el que carga los formatos, usuarios y perfumes de ejemplo. En una base compartida que ya tiene tablas de otras materias, `EnsureCreated` no crea nada: por eso `init.sql` es obligatorio.

6. Levantar el stack **sin** el override local (solo la API, conectada al PostgreSQL compartido):

   ```bash
   docker compose up -d --build
   ```

7. Verificar:

   ```bash
   docker compose ps
   docker logs -f perfumes_api
   curl http://localhost:5007/api/status
   curl http://localhost:5007/api/perfumes
   curl http://localhost:8087/api/kestrel-info
   ```

8. Firewall (UFW):

   ```bash
   sudo ufw default deny incoming
   sudo ufw default allow outgoing
   sudo ufw allow 22/tcp
   sudo ufw allow 5007/tcp
   sudo ufw allow 8087/tcp
   sudo ufw enable
   sudo ufw status
   ```

   > Si usás Nginx, permití solo `22`, `80` y `443` y no expongas `5007`/`8087` al exterior.

## DNS dinámico con DuckDNS

> Anexo opcional: esta parte **no se evalúa** en el examen. Se deja documentada por si se necesita más adelante.

1. Registrarse en <https://www.duckdns.org> y crear un subdominio (ej. `perfumes-nico.duckdns.org`).
2. Actualizar la IP del VPS desde el panel.
3. Opcional: script + cron para actualizar la IP automáticamente:

   ```bash
   mkdir -p ~/duckdns && cd ~/duckdns
   nano duck.sh
   ```

   ```bash
   #!/bin/bash
   echo url="https://www.duckdns.org/update?domains=TU_DOMINIO&token=TU_TOKEN&ip=" | curl -k -o ~/duckdns/duck.log -K -
   ```

   ```bash
   chmod 700 duck.sh
   ./duck.sh
   crontab -e
   # agregar:
   */5 * * * * ~/duckdns/duck.sh >/dev/null 2>&1
   ```

4. Verificar desde otra máquina:

   ```bash
   nslookup perfumes-nico.duckdns.org 8.8.8.8
   ```

## Nginx + HTTPS (opcional, suma para hardening)

Para centralizar el tráfico en 80/443 y no exponer los puertos internos:

```nginx
server {
    listen 80;
    server_name perfumes-nico.duckdns.org;

    location = /api/kestrel-info {
        proxy_pass http://127.0.0.1:8087/api/kestrel-info;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    location / {
        proxy_pass http://127.0.0.1:5007/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
sudo nano /etc/nginx/sites-available/perfumes
sudo ln -s /etc/nginx/sites-available/perfumes /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
sudo certbot --nginx -d perfumes-nico.duckdns.org
```

Con esto la API queda disponible por HTTPS en el dominio y UFW solo necesita `22`, `80` y `443`.

## Seguridad

- **Nunca** subir `.env` al repositorio (ya está en `.gitignore`).
- La cadena de conexión se inyecta con `ConnectionStrings__DefaultConnection`.
- `appsettings.json` no contiene credenciales.
- El PostgreSQL de desarrollo (override local) no forma parte del compose base y no se expone al exterior.
- En el VPS la API se conecta al PostgreSQL compartido indicado por `DB_HOST`; la base no se publica desde el compose.
- `/api/kestrel-info` solo responde por el puerto administrativo 8080.
- Usar una contraseña de base sin `;` ni espacios para evitar problemas en la cadena de conexión.

## Estructura

```text
.
├── Program.cs
├── AppDbContext.cs
├── PerfumesApi.csproj
├── global.json
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
├── init.sql
├── Dockerfile
├── docker-compose.yml
├── docker-compose.local.yml
├── .env.example
├── .gitignore
├── .gitattributes
├── .dockerignore
├── PerfumesApi.http
├── Properties/
│   └── launchSettings.json
└── README.md
```

## Solución de problemas

- **`POSTGRES_PASSWORD no definida`**: falta crear `.env` o completar la variable.
- **La API no conecta a la base (local)**: revisar `docker compose ps` y `docker logs perfumes_db`; esperar el healthcheck.
- **La API no conecta al PostgreSQL del VPS**: revisar `DB_HOST`, `DB_INTERNAL_PORT`, usuario, contraseña y nombre de la base. Si la base escucha en el host, usar `DB_HOST=host.docker.internal`.
- **No aparecen las tablas `udi2_*` en el VPS**: ejecutar `init.sql` contra la base asignada (paso 5 del despliegue).
- **Cambié el esquema y no se aplica en local**: `init.sql` solo corre con el volumen vacío; ejecutar `docker compose -f docker-compose.yml -f docker-compose.local.yml down -v` y volver a levantar.
- **Error de mayúsculas en Linux**: el archivo debe llamarse exactamente `Dockerfile` (no `dockerfile`).
- **Puerto ocupado**: cambiar `API_PORT`/`ADMIN_PORT` en `.env`.
- **`docker` no se reconoce en Windows**: el CLI está instalado pero su carpeta no está en el PATH. Abrir PowerShell y ejecutar una sola vez:
  ```powershell
  $bin = "$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin"
  [Environment]::SetEnvironmentVariable('Path', "$([Environment]::GetEnvironmentVariable('Path','User').TrimEnd(';'));$bin", 'User')
  ```
  Luego **cerrar y reabrir la terminal** (el PATH no se recarga en sesiones ya abiertas). En instalaciones per-machine la ruta es `C:\Program Files\Docker\Docker\resources\bin`.
- **Error `dockerDesktopLinuxEngine` / pipe not found**: el engine de Docker Desktop no está corriendo; abrir Docker Desktop y esperar a que indique que el engine está listo.
- **El puerto 5432 está ocupado**: el override local usa `DB_PORT=5433` justamente para no chocar con un PostgreSQL instalado en Windows. Si 5433 también está ocupado, cambiar `DB_PORT` en `.env`.
