using Microsoft.EntityFrameworkCore;
using SiakadKeuanganAPI.Data;
using SiakadKeuanganAPI.DTOs;
using SiakadKeuanganApi.Models;

namespace SiakadKeuanganAPI.Services
{
    public interface IKeuanganService
    {
        Task<List<TagihanUktDto>> GetSemuaTagihanAsync();
        Task<List<TagihanUktDto>> GetTagihanByNimAsync(string nim);
        Task<TagihanUktDto?> GetTagihanByIdAsync(int id);
        Task<TagihanUktDto> BuatTagihanAsync(CreateTagihanDto dto);
        Task<TagihanUktDto?> UpdateStatusTagihanAsync(int id, string status);
        Task<PembayaranDto> BayarTagihanAsync(CreatePembayaranDto dto);
        Task<List<PembayaranDto>> GetRiwayatPembayaranAsync(string nim);
        Task<List<PembayaranDto>> GetSemuaRiwayatPembayaranAsync();
        Task<object> GetRingkasanKeuanganAsync(string nim);
        Task<object> GetDashboardSummaryAsync();
    }

    public class KeuanganService : IKeuanganService
    {
        private readonly AppDbContext _db;

        public KeuanganService(AppDbContext db) => _db = db;

        // Helper: pastikan DateTime selalu UTC untuk PostgreSQL
        private static DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc) return dt;
            if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc); // Unspecified → UTC
        }

        public async Task<List<TagihanUktDto>> GetSemuaTagihanAsync()
        {
            return await _db.TagihanUkt
                .Include(t => t.Mahasiswa)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => MapTagihan(t))
                .ToListAsync();
        }

        public async Task<List<TagihanUktDto>> GetTagihanByNimAsync(string nim)
        {
            return await _db.TagihanUkt
                .Include(t => t.Mahasiswa)
                .Where(t => t.NimMahasiswa == nim)
                .OrderByDescending(t => t.TahunAkademik)
                .ThenByDescending(t => t.Semester)
                .Select(t => MapTagihan(t))
                .ToListAsync();
        }

        public async Task<TagihanUktDto?> GetTagihanByIdAsync(int id)
        {
            var t = await _db.TagihanUkt.Include(t => t.Mahasiswa)
                .FirstOrDefaultAsync(t => t.Id == id);
            return t == null ? null : MapTagihan(t);
        }

        public async Task<TagihanUktDto> BuatTagihanAsync(CreateTagihanDto dto)
        {
            var mahasiswa = await _db.Mahasiswa
                .FirstOrDefaultAsync(m => m.Nim == dto.NimMahasiswa)
                ?? throw new Exception($"Mahasiswa tidak ditemukan. Lakukan sinkronisasi terlebih dahulu.");

            // Cek duplikasi
            var existing = await _db.TagihanUkt.AnyAsync(t =>
                t.NimMahasiswa == dto.NimMahasiswa &&
                t.Semester == dto.Semester &&
                t.TahunAkademik == dto.TahunAkademik);
            if (existing)
                throw new Exception($"Tagihan semester {dto.Semester} tahun {dto.TahunAkademik} sudah ada untuk mahasiswa ini.");

            var nomorTagihan = $"UKT-{dto.TahunAkademik}-{dto.Semester:D2}-{mahasiswa.Id}";

            var tagihan = new TagihanUkt
            {
                NomorTagihan = nomorTagihan,
                MahasiswaId = mahasiswa.Id,
                NimMahasiswa = dto.NimMahasiswa,
                Semester = dto.Semester,
                TahunAkademik = dto.TahunAkademik,
                NilaiUkt = dto.NilaiUkt,
                GolonganUkt = dto.GolonganUkt,
                StatusTagihan = "Belum Bayar",
                TanggalTagihan = DateTime.UtcNow,
                JatuhTempo = ToUtc(dto.JatuhTempo), // ← fix UTC
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TagihanUkt.Add(tagihan);
            await _db.SaveChangesAsync();

            tagihan.Mahasiswa = mahasiswa;
            return MapTagihan(tagihan);
        }

        public async Task<TagihanUktDto?> UpdateStatusTagihanAsync(int id, string status)
        {
            var tagihan = await _db.TagihanUkt.Include(t => t.Mahasiswa)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tagihan == null) return null;

            tagihan.StatusTagihan = status;
            tagihan.UpdatedAt = DateTime.UtcNow;
            if (status == "Lunas") tagihan.TanggalLunas = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapTagihan(tagihan);
        }

        public async Task<PembayaranDto> BayarTagihanAsync(CreatePembayaranDto dto)
        {
            var tagihan = await _db.TagihanUkt.Include(t => t.Mahasiswa)
                .FirstOrDefaultAsync(t => t.Id == dto.TagihanUktId)
                ?? throw new Exception("Tagihan tidak ditemukan.");

            if (tagihan.StatusTagihan == "Lunas")
                throw new Exception("Tagihan sudah lunas.");

            var nomorTransaksi = $"TRX-{DateTime.UtcNow:yyyyMMddHHmmss}-{tagihan.Id}";

            var pembayaran = new RiwayatPembayaran
            {
                NomorTransaksi = nomorTransaksi,
                MahasiswaId = tagihan.MahasiswaId,
                TagihanUktId = tagihan.Id,
                JumlahBayar = dto.JumlahBayar,
                MetodePembayaran = dto.MetodePembayaran,
                StatusPembayaran = "Sukses",
                Keterangan = dto.Keterangan,
                TanggalBayar = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.RiwayatPembayaran.Add(pembayaran);

            // Hitung total bayar → update status otomatis
            var totalBayar = await _db.RiwayatPembayaran
                .Where(r => r.TagihanUktId == tagihan.Id && r.StatusPembayaran == "Sukses")
                .SumAsync(r => r.JumlahBayar);
            totalBayar += dto.JumlahBayar;

            tagihan.StatusTagihan = totalBayar >= tagihan.NilaiUkt ? "Lunas" : "Cicilan";
            if (tagihan.StatusTagihan == "Lunas") tagihan.TanggalLunas = DateTime.UtcNow;
            tagihan.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new PembayaranDto
            {
                Id = pembayaran.Id,
                NomorTransaksi = pembayaran.NomorTransaksi,
                NimMahasiswa = tagihan.NimMahasiswa,
                NamaMahasiswa = tagihan.Mahasiswa?.NamaLengkap ?? "",
                TagihanUktId = tagihan.Id,
                JumlahBayar = pembayaran.JumlahBayar,
                MetodePembayaran = pembayaran.MetodePembayaran,
                StatusPembayaran = pembayaran.StatusPembayaran,
                Keterangan = pembayaran.Keterangan,
                TanggalBayar = pembayaran.TanggalBayar
            };
        }

        public async Task<List<PembayaranDto>> GetRiwayatPembayaranAsync(string nim)
        {
            return await _db.RiwayatPembayaran
                .Include(r => r.Mahasiswa)
                .Where(r => r.Mahasiswa!.Nim == nim)
                .OrderByDescending(r => r.TanggalBayar)
                .Select(r => new PembayaranDto
                {
                    Id = r.Id,
                    NomorTransaksi = r.NomorTransaksi,
                    NimMahasiswa = r.Mahasiswa!.Nim,
                    NamaMahasiswa = r.Mahasiswa.NamaLengkap,
                    TagihanUktId = r.TagihanUktId,
                    JumlahBayar = r.JumlahBayar,
                    MetodePembayaran = r.MetodePembayaran,
                    StatusPembayaran = r.StatusPembayaran,
                    Keterangan = r.Keterangan,
                    TanggalBayar = r.TanggalBayar
                }).ToListAsync();
        }

        public async Task<List<PembayaranDto>> GetSemuaRiwayatPembayaranAsync()
        {
            return await _db.RiwayatPembayaran
                .Include(r => r.Mahasiswa)
                .OrderByDescending(r => r.TanggalBayar)
                .Select(r => new PembayaranDto
                {
                    Id = r.Id,
                    NomorTransaksi = r.NomorTransaksi,
                    NimMahasiswa = r.Mahasiswa!.Nim,
                    NamaMahasiswa = r.Mahasiswa.NamaLengkap,
                    TagihanUktId = r.TagihanUktId,
                    JumlahBayar = r.JumlahBayar,
                    MetodePembayaran = r.MetodePembayaran,
                    StatusPembayaran = r.StatusPembayaran,
                    Keterangan = r.Keterangan,
                    TanggalBayar = r.TanggalBayar
                }).ToListAsync();
        }

        public async Task<object> GetRingkasanKeuanganAsync(string nim)
        {
            var mahasiswa = await _db.Mahasiswa
                .Include(m => m.TagihanUkt)
                .FirstOrDefaultAsync(m => m.Nim == nim)
                ?? throw new Exception($"Mahasiswa tidak ditemukan.");

            var totalTagihan = mahasiswa.TagihanUkt.Sum(t => t.NilaiUkt);
            var totalBayar = await _db.RiwayatPembayaran
                .Where(r => r.MahasiswaId == mahasiswa.Id && r.StatusPembayaran == "Sukses")
                .SumAsync(r => r.JumlahBayar);
            var tunggakan = mahasiswa.TagihanUkt
                .Where(t => t.StatusTagihan != "Lunas")
                .Sum(t => t.NilaiUkt);

            return new
            {
                Nim = mahasiswa.Nim,
                Nama = mahasiswa.NamaLengkap,
                ProgramStudi = mahasiswa.ProgramStudi,
                StatusAkademik = mahasiswa.StatusAkademik,
                TotalTagihan = totalTagihan,
                TotalPembayaran = totalBayar,
                TotalTunggakan = tunggakan,
                JumlahTagihan = mahasiswa.TagihanUkt.Count,
                TagihanLunas = mahasiswa.TagihanUkt.Count(t => t.StatusTagihan == "Lunas"),
                TagihanBelumBayar = mahasiswa.TagihanUkt.Count(t => t.StatusTagihan == "Belum Bayar"),
            };
        }

        public async Task<object> GetDashboardSummaryAsync()
        {
            var totalMahasiswa = await _db.Mahasiswa.CountAsync();
            var totalTagihan = await _db.TagihanUkt.CountAsync();
            var tagihanLunas = await _db.TagihanUkt.CountAsync(t => t.StatusTagihan == "Lunas");
            var tagihanBelumBayar = await _db.TagihanUkt.CountAsync(t => t.StatusTagihan == "Belum Bayar");
            var totalPenerimaan = await _db.RiwayatPembayaran
                .Where(r => r.StatusPembayaran == "Sukses")
                .SumAsync(r => r.JumlahBayar);
            var totalTransaksi = await _db.RiwayatPembayaran.CountAsync();

            return new
            {
                TotalMahasiswa = totalMahasiswa,
                TotalTagihan = totalTagihan,
                TagihanLunas = tagihanLunas,
                TagihanBelumBayar = tagihanBelumBayar,
                TotalPenerimaan = totalPenerimaan,
                TotalTransaksi = totalTransaksi,
                PersentaseLunas = totalTagihan > 0
                    ? Math.Round((double)tagihanLunas / totalTagihan * 100, 2) : 0
            };
        }

        private static TagihanUktDto MapTagihan(TagihanUkt t) => new()
        {
            Id = t.Id,
            NomorTagihan = t.NomorTagihan,
            NimMahasiswa = t.NimMahasiswa,
            NamaMahasiswa = t.Mahasiswa?.NamaLengkap ?? "",
            Semester = t.Semester,
            TahunAkademik = t.TahunAkademik,
            NilaiUkt = t.NilaiUkt,
            GolonganUkt = t.GolonganUkt,
            StatusTagihan = t.StatusTagihan,
            TanggalTagihan = t.TanggalTagihan,
            JatuhTempo = t.JatuhTempo,
            TanggalLunas = t.TanggalLunas
        };
    }
}