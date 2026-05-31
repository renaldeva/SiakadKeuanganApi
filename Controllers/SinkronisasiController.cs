using Microsoft.AspNetCore.Mvc;
using SiakadKeuanganAPI.DTOs;
using SiakadKeuanganAPI.Services;

namespace SiakadKeuanganAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SinkronisasiController : ControllerBase
    {
        private readonly IMahasiswaSyncService _syncService;

        public SinkronisasiController(IMahasiswaSyncService syncService)
            => _syncService = syncService;

        /// <summary>Sinkronisasi data mahasiswa dari API Eksternal ke Database</summary>
        [HttpPost("mahasiswa")]
        public async Task<ActionResult<ApiResponse<SinkronisasiResultDto>>> SinkronMahasiswa()
        {
            var result = await _syncService.SinkronisasiDataAsync();
            return Ok(new ApiResponse<SinkronisasiResultDto>
            {
                Success = result.Sukses,
                Message = result.Pesan,
                Data = result
            });
        }

        /// <summary>Preview data dari API Mahasiswa tanpa menyimpan ke DB</summary>
        [HttpGet("preview")]
        public async Task<ActionResult<ApiResponse<List<MahasiswaApiData>>>> PreviewApiData()
        {
            var data = await _syncService.GetDataDariApiExternalAsync();
            return Ok(new ApiResponse<List<MahasiswaApiData>>
            {
                Success = true,
                Message = $"Berhasil mengambil {data.Count} data dari API Mahasiswa",
                Data = data,
                Total = data.Count
            });
        }
    }
}