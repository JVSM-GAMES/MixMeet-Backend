using Microsoft.EntityFrameworkCore;
using MixMeet.Reservas.Models;

namespace MixMeet.Reservas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Reserva> Reservas { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!; // <--- NOVO

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new { r.DataHoraInicio, r.DataHoraFim, r.Sala });
        }
    }
}