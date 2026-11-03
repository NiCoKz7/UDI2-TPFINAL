using Microsoft.EntityFrameworkCore;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<Perfume> Perfumes { get; set; } = new();
}

public class Formato
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<Perfume> Perfumes { get; set; } = new();
}

public class Perfume
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int FormatoId { get; set; }
    public Formato? Formato { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Formato> Formatos => Set<Formato>();
    public DbSet<Perfume> Perfumes => Set<Perfume>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Formato>()
            .HasIndex(f => f.Nombre)
            .IsUnique();

        modelBuilder.Entity<Perfume>()
            .Property(p => p.Precio)
            .HasPrecision(10, 2);
    }
}
