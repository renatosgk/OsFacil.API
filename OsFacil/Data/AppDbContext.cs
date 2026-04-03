using Microsoft.EntityFrameworkCore;
using OsFacil.Models;

namespace OsFacil.Data
{
    public class AppDbContext: DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }


        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Carro> Carros { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
        public DbSet<ItemServico> ItensServico { get; set; }
   
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
                foreach (var property in modelBuilder.Model.GetEntityTypes()
                    .SelectMany(t => t.GetProperties())
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                {
                    property.SetColumnType("decimal(18,2)");
                }

                
                modelBuilder.Entity<Carro>()
                    .HasOne(c => c.Usuario)
                    .WithMany(u => u.Carros) 
                    .HasForeignKey(c => c.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade); 

               
                modelBuilder.Entity<ItemServico>()
                    .HasOne(i => i.OrdemServico)
                    .WithMany(o => o.ItensServico) 
                    .HasForeignKey(i => i.OrdemServicoId);

                base.OnModelCreating(modelBuilder);
        }
    }
}
