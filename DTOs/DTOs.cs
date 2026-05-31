using Newtonsoft.Json;

namespace SiakadKeuanganAPI.DTOs
{
    // ─── Response dari API Mahasiswa Eksternal ──────────────────────────────
    public class MahasiswaApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<MahasiswaApiData>? Data { get; set; }
        public MahasiswaApiData? Mahasiswa { get; set; }
    }

    public class MahasiswaApiData
    {
        // MongoDB _id dipakai sebagai NIM pengganti
        [JsonProperty("_id")]
        public string? MongoId { get; set; }

        public int? Id { get; set; }
        public string? Nim { get; set; }
        public string? Nama { get; set; }
        public string? NamaLengkap { get; set; }
        public string? ProgramStudi { get; set; }
        public string? Prodi { get; set; }
        public string? Fakultas { get; set; }
        public int? Angkatan { get; set; }
        public string? StatusAkademik { get; set; }
        public string? Status { get; set; }
        public string? Email { get; set; }
        public string? NoHp { get; set; }
        public string? Phone { get; set; }
        public List<string>? MataKuliah { get; set; }
        public string? CreatedAt { get; set; }
    }

    // ─── DTOs Internal ──────────────────────────────────────────────────────
    public class MahasiswaDto
    {
        public int Id { get; set; }
        public string Nim { get; set; } = string.Empty;
        public string NamaLengkap { get; set; } = string.Empty;
        public string ProgramStudi { get; set; } = string.Empty;
        public string Fakultas { get; set; } = string.Empty;
        public int Angkatan { get; set; }
        public string StatusAkademik { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? NoHp { get; set; }
        public TagihanUktDto? TagihanAktif { get; set; }
        public decimal TotalTunggakan { get; set; }
    }

    public class TagihanUktDto
    {
        public int Id { get; set; }
        public string NomorTagihan { get; set; } = string.Empty;
        public string NimMahasiswa { get; set; } = string.Empty;
        public string NamaMahasiswa { get; set; } = string.Empty;
        public int Semester { get; set; }
        public int TahunAkademik { get; set; }
        public decimal NilaiUkt { get; set; }
        public string GolonganUkt { get; set; } = string.Empty;
        public string StatusTagihan { get; set; } = string.Empty;
        public DateTime TanggalTagihan { get; set; }
        public DateTime JatuhTempo { get; set; }
        public DateTime? TanggalLunas { get; set; }
    }

    public class CreateTagihanDto
    {
        public string NimMahasiswa { get; set; } = string.Empty;
        public int Semester { get; set; }
        public int TahunAkademik { get; set; }
        public decimal NilaiUkt { get; set; }
        public string GolonganUkt { get; set; } = "1";
        public DateTime JatuhTempo { get; set; }
    }

    public class PembayaranDto
    {
        public int Id { get; set; }
        public string NomorTransaksi { get; set; } = string.Empty;
        public string NimMahasiswa { get; set; } = string.Empty;
        public string NamaMahasiswa { get; set; } = string.Empty;
        public int TagihanUktId { get; set; }
        public decimal JumlahBayar { get; set; }
        public string MetodePembayaran { get; set; } = string.Empty;
        public string StatusPembayaran { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public DateTime TanggalBayar { get; set; }
    }

    public class CreatePembayaranDto
    {
        public int TagihanUktId { get; set; }
        public decimal JumlahBayar { get; set; }
        public string MetodePembayaran { get; set; } = "Transfer Bank";
        public string? Keterangan { get; set; }
    }

    public class SinkronisasiResultDto
    {
        public bool Sukses { get; set; }
        public string Pesan { get; set; } = string.Empty;
        public int JumlahDataDiambil { get; set; }
        public int JumlahDataBaru { get; set; }
        public int JumlahDataDiupdate { get; set; }
        public DateTime WaktuSinkron { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int? Total { get; set; }
    }
}