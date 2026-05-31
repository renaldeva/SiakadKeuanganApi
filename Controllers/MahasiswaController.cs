using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiakadKeuanganAPI.Data;
using SiakadKeuanganAPI.DTOs;

namespace SiakadKeuanganAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MahasiswaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MahasiswaController(AppDbContext db) => _db = db;

        /// <summary>Daftar semua mahasiswa yang telah disinkronisasi</summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<MahasiswaDto>>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? prodi,
            [FromQuery] string? status)
        {
            var query = _db.Mahasiswa.AsQueryable();

            // Pencarian hanya berdasarkan nama
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m =>
                    m.NamaLengkap.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrWhiteSpace(prodi))
                query = query.Where(m => m.ProgramStudi.Contains(prodi));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(m => m.StatusAkademik == status);

            var data = await query.OrderBy(m => m.NamaLengkap).ToListAsync();

            var result = data.Select(m => new MahasiswaDto
            {
                Id = m.Id,
                Nim = m.Nim,
                NamaLengkap = m.NamaLengkap,
                ProgramStudi = m.ProgramStudi,
                Fakultas = m.Fakultas,
                Angkatan = m.Angkatan,
                StatusAkademik = m.StatusAkademik,
                Email = m.Email,
                NoHp = m.NoHp
            }).ToList();

            return Ok(new ApiResponse<List<MahasiswaDto>>
            {
                Success = true,
                Message = "Berhasil",
                Data = result,
                Total = result.Count
            });
        }

        /// <summary>Detail mahasiswa berdasarkan ID internal</summary>
        [HttpGet("{nim}")]
        public async Task<ActionResult<ApiResponse<MahasiswaDto>>> GetByNim(string nim)
        {
            var m = await _db.Mahasiswa
                .Include(x => x.TagihanUkt)
                .FirstOrDefaultAsync(x => x.Nim == nim);

            if (m == null)
                return NotFound(new ApiResponse<MahasiswaDto>
                {
                    Success = false,
                    Message = $"Mahasiswa tidak ditemukan"
                });

            var dto = new MahasiswaDto
            {
                Id = m.Id,
                Nim = m.Nim,
                NamaLengkap = m.NamaLengkap,
                ProgramStudi = m.ProgramStudi,
                Fakultas = m.Fakultas,
                Angkatan = m.Angkatan,
                StatusAkademik = m.StatusAkademik,
                Email = m.Email,
                NoHp = m.NoHp,
                TotalTunggakan = m.TagihanUkt
                    .Where(t => t.StatusTagihan != "Lunas")
                    .Sum(t => t.NilaiUkt)
            };

            return Ok(new ApiResponse<MahasiswaDto>
            {
                Success = true,
                Message = "Berhasil",
                Data = dto
            });
        }
    }
}