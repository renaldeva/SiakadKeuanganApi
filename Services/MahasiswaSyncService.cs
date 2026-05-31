using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SiakadKeuanganAPI.Data;
using SiakadKeuanganApi.Models;
using SiakadKeuanganAPI.DTOs;

namespace SiakadKeuanganAPI.Services
{
    public interface IMahasiswaSyncService
    {
        Task<SinkronisasiResultDto> SinkronisasiDataAsync();
        Task<List<MahasiswaApiData>> GetDataDariApiExternalAsync();
    }

    public class MahasiswaSyncService : IMahasiswaSyncService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<MahasiswaSyncService> _logger;

        public MahasiswaSyncService(
            AppDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<MahasiswaSyncService> logger)
        {
            _db = db;
            _httpClient = httpClientFactory.CreateClient("MahasiswaApi");
            _config = config;
            _logger = logger;
        }

        public async Task<List<MahasiswaApiData>> GetDataDariApiExternalAsync()
        {
            var baseUrl = _config["MahasiswaApi:BaseUrl"]
                ?? "https://mahasiswa-api-psi.vercel.app";
            var endpoint = _config["MahasiswaApi:Endpoint"]
                ?? "/api/mahasiswa";

            var url = $"{baseUrl.TrimEnd('/')}{endpoint}";
            _logger.LogInformation("Mengambil data dari: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response length: {Len}", json.Length);

            // Parse sebagai object wrapper { success, data: [...] }
            var wrapper = JsonConvert.DeserializeObject<MahasiswaApiResponse>(json);
            if (wrapper?.Data != null && wrapper.Data.Count > 0)
                return wrapper.Data;

            // Fallback: array langsung
            if (json.TrimStart().StartsWith('['))
            {
                var list = JsonConvert.DeserializeObject<List<MahasiswaApiData>>(json);
                return list ?? new List<MahasiswaApiData>();
            }

            return new List<MahasiswaApiData>();
        }

        public async Task<SinkronisasiResultDto> SinkronisasiDataAsync()
        {
            var log = new SinkronisasiLog { TanggalSinkron = DateTime.UtcNow };
            int baru = 0, diupdate = 0;

            try
            {
                var dataApi = await GetDataDariApiExternalAsync();
                log.JumlahDataDiambil = dataApi.Count;
                _logger.LogInformation("Total data dari API: {Count}", dataApi.Count);

                foreach (var item in dataApi)
                {
                    // ✅ Prioritas NIM: nim > _id (MongoDB) > id
                    var nim = item.Nim
                        ?? item.MongoId
                        ?? item.Id?.ToString()
                        ?? "";

                    if (string.IsNullOrWhiteSpace(nim))
                    {
                        _logger.LogWarning("Data dilewati karena tidak ada identifier");
                        continue;
                    }

                    // ✅ Nama: nama > namaLengkap
                    var namaLengkap = item.Nama
                        ?? item.NamaLengkap
                        ?? "Tidak Diketahui";

                    var prodi = item.ProgramStudi ?? item.Prodi ?? "";
                    var status = item.StatusAkademik ?? item.Status ?? "Aktif";
                    var noHp = item.NoHp ?? item.Phone;
                    var angkatan = item.Angkatan ?? DateTime.Now.Year;

                    _logger.LogInformation("Proses: NIM={Nim}, Nama={Nama}", nim, namaLengkap);

                    var existing = await _db.Mahasiswa
                        .FirstOrDefaultAsync(m => m.Nim == nim);

                    if (existing == null)
                    {
                        _db.Mahasiswa.Add(new Mahasiswa
                        {
                            Nim = nim,
                            NamaLengkap = namaLengkap,
                            ProgramStudi = prodi,
                            Fakultas = item.Fakultas ?? "",
                            Angkatan = angkatan,
                            StatusAkademik = status,
                            Email = item.Email,
                            NoHp = noHp,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                        baru++;
                    }
                    else
                    {
                        existing.NamaLengkap = namaLengkap;
                        existing.ProgramStudi = prodi;
                        existing.Fakultas = item.Fakultas ?? existing.Fakultas;
                        existing.Angkatan = angkatan;
                        existing.StatusAkademik = status;
                        existing.Email = item.Email ?? existing.Email;
                        existing.NoHp = noHp ?? existing.NoHp;
                        existing.UpdatedAt = DateTime.UtcNow;
                        diupdate++;
                    }
                }

                await _db.SaveChangesAsync();

                log.JumlahDataBaru = baru;
                log.JumlahDataDiupdate = diupdate;
                log.Sukses = true;
                _db.SinkronisasiLog.Add(log);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Sinkronisasi selesai: {Baru} baru, {Update} diupdate", baru, diupdate);

                return new SinkronisasiResultDto
                {
                    Sukses = true,
                    Pesan = $"Sinkronisasi berhasil: {baru} data baru, {diupdate} diperbarui",
                    JumlahDataDiambil = dataApi.Count,
                    JumlahDataBaru = baru,
                    JumlahDataDiupdate = diupdate,
                    WaktuSinkron = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal sinkronisasi: {Message}", ex.Message);
                log.Sukses = false;
                log.PesanError = ex.Message;
                _db.SinkronisasiLog.Add(log);
                await _db.SaveChangesAsync();

                return new SinkronisasiResultDto
                {
                    Sukses = false,
                    Pesan = $"Sinkronisasi gagal: {ex.Message}",
                    WaktuSinkron = DateTime.UtcNow
                };
            }
        }
    }
}