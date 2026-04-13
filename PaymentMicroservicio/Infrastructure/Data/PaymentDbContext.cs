using Microsoft.EntityFrameworkCore;
using PaymentMicroservicio.Domain.Entities;

namespace PaymentMicroservicio.Infrastructure.Data;

/// <summary>
/// DbContext para el microservicio de pagos
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentItem> PaymentItems { get; set; }
    public DbSet<PaymentReceipt> PaymentReceipts { get; set; }
    public DbSet<ServiceCatalog> ServiceCatalogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StripeSessionId).IsUnique().HasDatabaseName("UQ_Payment_stripe_session_id");
            entity.HasIndex(e => e.IdEstudiante);
            entity.HasIndex(e => e.IdPeriodo);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PaymentType);
            entity.HasIndex(e => e.Procesado);
            // Composite index for the most critical query: VerificarMatriculaPagada
            entity.HasIndex(e => new { e.IdEstudiante, e.IdPeriodo, e.Status, e.PaymentType })
                  .HasDatabaseName("IX_Payment_Verificacion");

            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.PaymentType).HasMaxLength(50);
        });

        // PaymentItem configuration
        modelBuilder.Entity<PaymentItem>(entity =>
        {
            entity.ToTable("PaymentItem");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdPayment);
            entity.HasIndex(e => e.IdCurso);

            entity.Property(e => e.PrecioUnitario).HasPrecision(10, 2);
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);

            entity.HasOne(e => e.Payment)
                .WithMany(p => p.PaymentItems)
                .HasForeignKey(e => e.IdPayment)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PaymentReceipt configuration
        modelBuilder.Entity<PaymentReceipt>(entity =>
        {
            entity.ToTable("PaymentReceipt");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptCode).IsUnique().HasDatabaseName("UQ_PaymentReceipt_receipt_code");
            entity.HasIndex(e => e.StripeSessionId).IsUnique().HasDatabaseName("UQ_PaymentReceipt_stripe_session_id");
            entity.HasIndex(e => e.StripeEventId).IsUnique().HasDatabaseName("UQ_PaymentReceipt_stripe_event_id");
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.StudentCode);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        // ServiceCatalog configuration
        modelBuilder.Entity<ServiceCatalog>(entity =>
        {
            entity.ToTable("ServiceCatalog");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Activo);
            entity.Property(e => e.Monto).HasPrecision(10, 2);

            entity.HasData(
                new ServiceCatalog
                {
                    Id = 1,
                    Code = "matricula",
                    Nombre = "RESERVA DE MATRÍCULA POR SEMESTRE (PREGRADO)",
                    Descripcion = "Registrar en el sistema académico sus cursos",
                    Detalle = "Costo por c/reserva de matrícula por semestre (pregrado)",
                    Monto = 5.00m,
                    Categoria = "Matrícula",
                    TipoPago = "matricula",
                    Activo = true,
                    Orden = 1,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 2,
                    Code = "seguimiento-curricular",
                    Nombre = "SEGUIMIENTO CURRICULAR PREGRADO",
                    Descripcion = "Trámite de seguimiento curricular",
                    Detalle = "Seguimiento y verificación del avance curricular",
                    Monto = 15.00m,
                    Categoria = "Trámites",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 2,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 3,
                    Code = "constancia-matricula",
                    Nombre = "CONSTANCIA DE MATRÍCULA",
                    Descripcion = "Documento oficial de matrícula vigente",
                    Detalle = "Emisión de constancia de matrícula del período actual",
                    Monto = 10.00m,
                    Categoria = "Constancias",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 3,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 4,
                    Code = "constancia-notas",
                    Nombre = "CONSTANCIA DE NOTAS",
                    Descripcion = "Registro oficial de calificaciones",
                    Detalle = "Emisión de constancia con historial de notas",
                    Monto = 15.00m,
                    Categoria = "Constancias",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 4,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 5,
                    Code = "carnet-duplicado",
                    Nombre = "CARNET UNIVERSITARIO - DUPLICADO",
                    Descripcion = "Trámite de duplicado de carnet",
                    Detalle = "Solicitud de duplicado del carnet universitario",
                    Monto = 25.00m,
                    Categoria = "Trámites",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 5,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 6,
                    Code = "examen-culminacion",
                    Nombre = "EXAMEN EXCEPCIONAL - CULMINACIÓN DE PLAN DE ESTUDIOS PREGRADO",
                    Descripcion = "Tramitar",
                    Detalle = "Examen excepcional para culminación de plan de estudios",
                    Monto = 105.00m,
                    Categoria = "Exámenes",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 6,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 7,
                    Code = "certificado-estudios",
                    Nombre = "CERTIFICADO DE ESTUDIOS",
                    Descripcion = "Documento oficial de estudios realizados",
                    Detalle = "Emisión de certificado oficial de estudios completos",
                    Monto = 50.00m,
                    Categoria = "Constancias",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 7,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ServiceCatalog
                {
                    Id = 8,
                    Code = "constancia-egresado",
                    Nombre = "CONSTANCIA DE EGRESADO",
                    Descripcion = "Documento que acredita condición de egresado",
                    Detalle = "Emisión de constancia de egresado de la universidad",
                    Monto = 20.00m,
                    Categoria = "Constancias",
                    TipoPago = "servicio",
                    Activo = true,
                    Orden = 8,
                    FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}
