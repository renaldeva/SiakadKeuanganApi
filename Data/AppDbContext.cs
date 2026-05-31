using Microsoft.EntityFrameworkCore;
using SiakadKeuanganApi.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SiakadKeuanganAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Mahasiswa> Mahasiswa { get; set; }
        public DbSet<TagihanUkt> TagihanUkt { get; set; }
        public DbSet<RiwayatPembayaran> RiwayatPembayaran { get; set; }
        public DbSet<SinkronisasiLog> SinkronisasiLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index unik NIM
            modelBuilder.Entity<Mahasiswa>()
                .HasIndex(m => m.Nim)
                .IsUnique();

            // Index unik nomor tagihan
            modelBuilder.Entity<TagihanUkt>()
                .HasIndex(t => t.NomorTagihan)
                .IsUnique();

            // Index unik per mahasiswa per semester per tahun
            modelBuilder.Entity<TagihanUkt>()
                .HasIndex(t => new { t.NimMahasiswa, t.Semester, t.TahunAkademik })
                .IsUnique();

            // Index unik nomor transaksi
            modelBuilder.Entity<RiwayatPembayaran>()
                .HasIndex(r => r.NomorTransaksi)
                .IsUnique();

            // Relasi
            modelBuilder.Entity<TagihanUkt>()
                .HasOne(t => t.Mahasiswa)
                .WithMany(m => m.TagihanUkt)
                .HasForeignKey(t => t.MahasiswaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RiwayatPembayaran>()
                .HasOne(r => r.Mahasiswa)
                .WithMany(m => m.RiwayatPembayaran)
                .HasForeignKey(r => r.MahasiswaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RiwayatPembayaran>()
                .HasOne(r => r.TagihanUkt)
                .WithMany(t => t.RiwayatPembayaran)
                .HasForeignKey(r => r.TagihanUktId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}