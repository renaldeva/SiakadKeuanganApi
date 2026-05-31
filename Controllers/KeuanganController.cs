using Microsoft.AspNetCore.Mvc;
using SiakadKeuanganAPI.DTOs;
using SiakadKeuanganAPI.Services;

namespace SiakadKeuanganAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KeuanganController : ControllerBase
    {
        private readonly IKeuanganService _keuanganService;

        public KeuanganController(IKeuanganService keuanganService)
            => _keuanganService = keuanganService;

        // ─── TAGIHAN UKT ────────────────────────────────────────────────────

        /// <summary>Semua tagihan UKT</summary>
        [HttpGet("tagihan")]
        public async Task<ActionResult<ApiResponse<List<TagihanUktDto>>>> GetSemuaTagihan()
        {
            var data = await _keuanganService.GetSemuaTagihanAsync();
            return Ok(new ApiResponse<List<TagihanUktDto>>
            {
                Success = true,
                Message = "Berhasil",
                Data = data,
                Total = data.Count
            });
        }

        /// <summary>Tagihan berdasarkan NIM mahasiswa</summary>
        [HttpGet("tagihan/mahasiswa/{nim}")]
        public async Task<ActionResult<ApiResponse<List<TagihanUktDto>>>> GetTagihanByNim(string nim)
        {
            var data = await _keuanganService.GetTagihanByNimAsync(nim);
            return Ok(new ApiResponse<List<TagihanUktDto>>
            {
                Success = true,
                Message = $"Tagihan mahasiswa NIM {nim}",
                Data = data,
                Total = data.Count
            });
        }

        /// <summary>Detail tagihan berdasarkan ID</summary>
        [HttpGet("tagihan/{id}")]
        public async Task<ActionResult<ApiResponse<TagihanUktDto>>> GetTagihanById(int id)
        {
            var data = await _keuanganService.GetTagihanByIdAsync(id);
            if (data == null)
                return NotFound(new ApiResponse<TagihanUktDto>
                { Success = false, Message = "Tagihan tidak ditemukan" });

            return Ok(new ApiResponse<TagihanUktDto>
            { Success = true, Message = "Berhasil", Data = data });
        }

        /// <summary>Buat tagihan UKT baru</summary>
        [HttpPost("tagihan")]
        public async Task<ActionResult<ApiResponse<TagihanUktDto>>> BuatTagihan(
            [FromBody] CreateTagihanDto dto)
        {
            try
            {
                var result = await _keuanganService.BuatTagihanAsync(dto);
                return Created($"/api/keuangan/tagihan/{result.Id}",
                    new ApiResponse<TagihanUktDto>
                    {
                        Success = true,
                        Message = "Tagihan berhasil dibuat",
                        Data = result
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<TagihanUktDto>
                { Success = false, Message = ex.Message });
            }
        }

        /// <summary>Update status tagihan</summary>
        [HttpPatch("tagihan/{id}/status")]
        public async Task<ActionResult<ApiResponse<TagihanUktDto>>> UpdateStatus(
            int id, [FromBody] UpdateStatusRequest req)
        {
            var result = await _keuanganService.UpdateStatusTagihanAsync(id, req.Status);
            if (result == null)
                return NotFound(new ApiResponse<TagihanUktDto>
                { Success = false, Message = "Tagihan tidak ditemukan" });

            return Ok(new ApiResponse<TagihanUktDto>
            { Success = true, Message = "Status diperbarui", Data = result });
        }

        // ─── PEMBAYARAN ──────────────────────────────────────────────────────

        /// <summary>Bayar tagihan UKT</summary>
        [HttpPost("pembayaran")]
        public async Task<ActionResult<ApiResponse<PembayaranDto>>> BayarTagihan(
            [FromBody] CreatePembayaranDto dto)
        {
            try
            {
                var result = await _keuanganService.BayarTagihanAsync(dto);
                return Created($"/api/keuangan/pembayaran",
                    new ApiResponse<PembayaranDto>
                    {
                        Success = true,
                        Message = "Pembayaran berhasil dicatat",
                        Data = result
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<PembayaranDto>
                { Success = false, Message = ex.Message });
            }
        }

        /// <summary>Riwayat pembayaran berdasarkan NIM</summary>
        [HttpGet("pembayaran/mahasiswa/{nim}")]
        public async Task<ActionResult<ApiResponse<List<PembayaranDto>>>> GetRiwayatByNim(string nim)
        {
            var data = await _keuanganService.GetRiwayatPembayaranAsync(nim);
            return Ok(new ApiResponse<List<PembayaranDto>>
            {
                Success = true,
                Message = "Berhasil",
                Data = data,
                Total = data.Count
            });
        }

        /// <summary>Semua riwayat pembayaran</summary>
        [HttpGet("pembayaran")]
        public async Task<ActionResult<ApiResponse<List<PembayaranDto>>>> GetSemuaRiwayat()
        {
            var data = await _keuanganService.GetSemuaRiwayatPembayaranAsync();
            return Ok(new ApiResponse<List<PembayaranDto>>
            {
                Success = true,
                Message = "Berhasil",
                Data = data,
                Total = data.Count
            });
        }

        // ─── RINGKASAN / DASHBOARD ───────────────────────────────────────────

        /// <summary>Ringkasan keuangan mahasiswa</summary>
        [HttpGet("ringkasan/{nim}")]
        public async Task<ActionResult<ApiResponse<object>>> GetRingkasan(string nim)
        {
            try
            {
                var data = await _keuanganService.GetRingkasanKeuanganAsync(nim);
                return Ok(new ApiResponse<object>
                { Success = true, Message = "Berhasil", Data = data });
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<object>
                { Success = false, Message = ex.Message });
            }
        }

        /// <summary>Dashboard summary keseluruhan</summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboard()
        {
            var data = await _keuanganService.GetDashboardSummaryAsync();
            return Ok(new ApiResponse<object>
            { Success = true, Message = "Berhasil", Data = data });
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}