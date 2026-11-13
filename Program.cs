using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Kestrel escucha en dos puertos: 5000 (público) y 8080 (administración/diagnóstico).
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.ListenAnyIP(8080);
});

var app = builder.Build();
// Si la base no existe o está completamente vacía, EF crea el esquema.
// En el VPS la base ya existe y puede tener tablas de otras materias,
// por eso las tablas udi2_* y los datos de prueba se cargan con init.sql.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Middleware de hardening: /api/kestrel-info solo es accesible por el puerto administrativo.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    if (!string.IsNullOrEmpty(path) &&
        path.Equals("/api/kestrel-info", StringComparison.OrdinalIgnoreCase) &&
        context.Connection.LocalPort != 8080)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Acceso denegado: endpoint accesible por el puerto administrativo (8080).");
        return;
    }

    await next();
});

// Diagnóstico de entorno (Unidad 1).
app.MapGet("/api/status", (IConfiguration config, IHostEnvironment env) =>
{
    var response = new
    {
        AppName = config["AppSettings:AppName"],
        Environment = env.EnvironmentName,
        EnableDetailedLogs = config.GetValue<bool>("AppSettings:FeatureFlag_EnableDetailedLogs"),
        DatabaseTimeout = config["DATABASE_TIMEOUT"] ?? "No definida"
    };

    return Results.Ok(response);
});

// Diagnóstico de Kestrel y ThreadPool (Unidad 2).
app.MapGet("/api/kestrel-info", () =>
{
    ThreadPool.GetAvailableThreads(out int workerAvailable, out int completionAvailable);
    ThreadPool.GetMaxThreads(out int workerMax, out int completionMax);

    using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
    double workingSetMb = Math.Round(currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2);

    var response = new
    {
        ThreadPool = new
        {
            WorkerThreads = new { Available = workerAvailable, Max = workerMax },
            CompletionPortThreads = new { Available = completionAvailable, Max = completionMax }
        },
        Memory = new { WorkingSetMB = workingSetMb },
        System = new { LogicalProcessors = Environment.ProcessorCount }
    };

    return Results.Ok(response);
});


// Endpoints obligatorios del TP Final (6)

// 1) Listar usuarios
app.MapGet("/api/usuarios", async (AppDbContext db) =>
{
    var usuarios = await db.Usuarios
        .AsNoTracking()
        .OrderBy(u => u.Id)
        .Select(u => new { u.Id, u.Nombre, u.Email })
        .ToListAsync();

    return Results.Ok(usuarios);
});

// 2) Listar formatos (Tubo / Miniatura / Botella)
app.MapGet("/api/formatos", async (AppDbContext db) =>
{
    var formatos = await db.Formatos
        .AsNoTracking()
        .OrderBy(f => f.Id)
        .Select(f => new { f.Id, f.Nombre })
        .ToListAsync();

    return Results.Ok(formatos);
});

// 3) Listar perfumes con filtros opcionales por usuario y formato
app.MapGet("/api/perfumes", async (int? usuarioId, int? formatoId, AppDbContext db) =>
{
    var query = db.Perfumes.AsNoTracking();

    if (usuarioId.HasValue)
        query = query.Where(p => p.UsuarioId == usuarioId.Value);

    if (formatoId.HasValue)
        query = query.Where(p => p.FormatoId == formatoId.Value);

    var perfumes = await query
        .OrderBy(p => p.Id)
        .Select(p => new
        {
            p.Id,
            p.Nombre,
            p.Marca,
            p.Precio,
            Usuario = new { p.Usuario!.Id, p.Usuario.Nombre },
            Formato = new { p.Formato!.Id, p.Formato.Nombre }
        })
        .ToListAsync();

    return Results.Ok(perfumes);
});

// 4) Crear perfume
app.MapPost("/api/perfumes", async (PerfumeRequest request, AppDbContext db) =>
{
    var error = await ValidarPerfumeAsync(request, db);
    if (error is not null)
        return Results.BadRequest(error);

    var perfume = new Perfume
    {
        Nombre = request.Nombre.Trim(),
        Marca = request.Marca.Trim(),
        Precio = request.Precio,
        UsuarioId = request.UsuarioId,
        FormatoId = request.FormatoId
    };

    db.Perfumes.Add(perfume);
    await db.SaveChangesAsync();

    return Results.Created($"/api/perfumes/{perfume.Id}", new
    {
        perfume.Id,
        perfume.Nombre,
        perfume.Marca,
        perfume.Precio,
        perfume.UsuarioId,
        perfume.FormatoId
    });
});

// 5) Actualizar perfume
app.MapPut("/api/perfumes/{id:int}", async (int id, PerfumeRequest request, AppDbContext db) =>
{
    var perfume = await db.Perfumes.FindAsync(id);
    if (perfume is null)
        return Results.NotFound();

    var error = await ValidarPerfumeAsync(request, db);
    if (error is not null)
        return Results.BadRequest(error);

    perfume.Nombre = request.Nombre.Trim();
    perfume.Marca = request.Marca.Trim();
    perfume.Precio = request.Precio;
    perfume.UsuarioId = request.UsuarioId;
    perfume.FormatoId = request.FormatoId;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// 6) Eliminar perfume
app.MapDelete("/api/perfumes/{id:int}", async (int id, AppDbContext db) =>
{
    var perfume = await db.Perfumes.FindAsync(id);
    if (perfume is null)
        return Results.NotFound();

    db.Perfumes.Remove(perfume);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

static async Task<string?> ValidarPerfumeAsync(PerfumeRequest request, AppDbContext db)
{
    if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Marca))
        return "El nombre y la marca son obligatorios.";

    if (request.Precio <= 0)
        return "El precio debe ser mayor a 0.";

    if (!await db.Usuarios.AnyAsync(u => u.Id == request.UsuarioId))
        return "El usuario indicado no existe.";

    if (!await db.Formatos.AnyAsync(f => f.Id == request.FormatoId))
        return "El formato indicado no existe.";

    return null;
}

app.Run();

public record PerfumeRequest(string Nombre, string Marca, decimal Precio, int UsuarioId, int FormatoId);
