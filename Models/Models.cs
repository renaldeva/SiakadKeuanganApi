using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SiakadKeuanganApi.Models
{
    // ─── Data yang di-sync dari API Mahasiswa ───────────────────────────────
    [Table("mahasiswa")]
    public class Mahasiswa
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Nim { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string NamaLengkap { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ProgramStudi { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Fakultas { get; set; } = string.Empty;

        public int Angkatan { get; set; }

        [MaxLength(20)]
        public string StatusAkademik { get; set; } = "Aktif"; // Aktif, Cuti, Lulus, DO

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? NoHp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<TagihanUkt> TagihanUkt { get; set; } = new List<TagihanUkt>();
        public ICollection<RiwayatPembayaran> RiwayatPembayaran { get; set; } = new List<RiwayatPembayaran>();
    }

    // ─── Data Keuangan ──────────────────────────────────────────────────────
    [Table("tagihan_ukt")]
    public class TagihanUkt
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string NomorTagihan { get; set; } = string.Empty;

        public int MahasiswaId { get; set; }

        [Required, MaxLength(50)]
        public string NimMahasiswa { get; set; } = string.Empty;

        public int Semester { get; set; }      // 1–14
        public int TahunAkademik { get; set; } // e.g. 2024

        [Column(TypeName = "numeric(15,2)")]
        public decimal NilaiUkt { get; set; }

        [MaxLength(20)]
        public string GolonganUkt { get; set; } = "1"; // 1–8

        [MaxLength(20)]
        public string StatusTagihan { get; set; } = "Belum Bayar";
        // Belum Bayar | Lunas | Cicilan | Terlambat

        public DateTime TanggalTagihan { get; set; }
        public DateTime JatuhTempo { get; set; }
        public DateTime? TanggalLunas { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("MahasiswaId")]
        public Mahasiswa? Mahasiswa { get; set; }
        public ICollection<RiwayatPembayaran> RiwayatPembayaran { get; set; } = new List<RiwayatPembayaran>();
    }

    [Table("riwayat_pembayaran")]
    public class RiwayatPembayaran
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string NomorTransaksi { get; set; } = string.Empty;

        public int MahasiswaId { get; set; }
        public int TagihanUktId { get; set; }

        [Column(TypeName = "numeric(15,2)")]
        public decimal JumlahBayar { get; set; }

        [MaxLength(50)]
        public string MetodePembayaran { get; set; } = "Transfer Bank";
        // Transfer Bank | Virtual Account | Tunai | QRIS

        [MaxLength(20)]
        public string StatusPembayaran { get; set; } = "Sukses";
        // Sukses | Gagal | Pending

        [MaxLength(100)]
        public string? Keterangan { get; set; }

        public DateTime TanggalBayar { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("MahasiswaId")]
        public Mahasiswa? Mahasiswa { get; set; }
        [ForeignKey("TagihanUktId")]
        public TagihanUkt? TagihanUkt { get; set; }
    }

    [Table("sinkronisasi_log")]
    public class SinkronisasiLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime TanggalSinkron { get; set; } = DateTime.UtcNow;
        public int JumlahDataDiambil { get; set; }
        public int JumlahDataBaru { get; set; }
        public int JumlahDataDiupdate { get; set; }
        public bool Sukses { get; set; }
        [MaxLength(500)]
        public string? PesanError { get; set; }
    }
}