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
    }
}
