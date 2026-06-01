# SIAKAD Keuangan — Backend API

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-UI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)

**REST API untuk Sistem Informasi Akademik Keuangan**  
Sinkronisasi data mahasiswa dari API eksternal · Pengelolaan tagihan UKT · Riwayat pembayaran

</div>

---

## Daftar Isi

- [Tentang Proyek](#tentang-proyek)
- [Arsitektur](#arsitektur)
- [Struktur Project](#struktur-project)
- [Prasyarat](#prasyarat)
- [Instalasi & Menjalankan](#instalasi--menjalankan)
  - [Docker Compose](#1-docker-compose-direkomendasikan)
  - [Manual](#2-manual)
- [Konfigurasi](#konfigurasi)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)
- [Sinkronisasi Data](#sinkronisasi-data)

---

## Tentang Proyek

SIAKAD Keuangan API adalah backend layanan keuangan akademik yang dibangun menggunakan **.NET 8** dan **PostgreSQL**. API ini bertugas:

- **Menyinkronkan** data mahasiswa dari API Mahasiswa eksternal secara dinamis
- **Mengelola** tagihan UKT per semester per mahasiswa
- **Mencatat** riwayat pembayaran secara real-time
- **Memperbarui** status tagihan otomatis berdasarkan akumulasi pembayaran

> Sumber data mahasiswa: [`https://mahasiswa-api-psi.vercel.app`](https://mahasiswa-api-psi.vercel.app)

---

## Arsitektur

```
Flutter App
    │
    ▼ HTTP / REST
┌─────────────────────────────────────────┐
│           SIAKAD Keuangan API           │
│  ┌─────────────┐   ┌─────────────────┐  │
│  │ Controllers │   │    Services     │  │
│  │  Mahasiswa  │   │  SyncService    │  │
│  │  Keuangan   │──▶│  KeuanganSvc    │  │
│  │  Sinkron    │   └────────┬────────┘  │
│  └─────────────┘            │           │
│                    ┌────────▼────────┐  │
│                    │   PostgreSQL    │  │
│                    │  AppDbContext   │  │
│                    └─────────────────┘  │
└─────────────────────────────────────────┘
    │
    ▼ HTTP GET (dinamis dari config)
API Mahasiswa Eksternal
https://mahasiswa-api-psi.vercel.app
```

---

## Struktur Project

```
SiakadKeuangan.API/
├── Controllers/
│   ├── MahasiswaController.cs       # Endpoint data mahasiswa
│   ├── KeuanganController.cs        # Endpoint tagihan & pembayaran
│   └── SinkronisasiController.cs    # Endpoint sinkronisasi API
├── Models/
│   └── Models.cs                    # Entity: Mahasiswa, TagihanUkt,
│                                    #   RiwayatPembayaran, SinkronisasiLog
├── DTOs/
│   └── DTOs.cs                      # Data Transfer Objects + API response wrapper
├── Data/
│   └── AppDbContext.cs              # EF Core DbContext + relasi antar tabel
├── Services/
│   ├── MahasiswaSyncService.cs      # Sinkronisasi dari API Mahasiswa eksternal
│   └── KeuanganService.cs          # Business logic: tagihan, pembayaran, dashboard
├── Migrations/                      # EF Core database migrations
├── Dockerfile                       # Container image untuk deployment
├── docker-compose.yml               # Orkestrasi PostgreSQL + API
├── appsettings.json                 # Konfigurasi production
├── appsettings.Development.json     # Konfigurasi development
└── Program.cs                       # Entry point + DI registration
```

---

## Prasyarat

| Kebutuhan | Versi | Keterangan |
|-----------|-------|------------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0+ | Untuk menjalankan secara manual |
| [PostgreSQL](https://www.postgresql.org/download/) | 15+ | Untuk menjalankan secara manual |
| [Docker](https://www.docker.com/get-started/) | 24+ | Untuk Docker Compose |
| [Docker Compose](https://docs.docker.com/compose/) | 2.x | Untuk Docker Compose |

---

## Instalasi & Menjalankan

### 1. Docker Compose (Direkomendasikan)

Cara paling mudah — PostgreSQL dan API berjalan otomatis dalam satu perintah.

```bash
# Clone repository
git clone https://github.com/renaldeva/siakad-keuangan.git
cd siakad-keuangan

# Jalankan
docker-compose up -d

# Cek status
docker-compose ps

# Lihat log
docker-compose logs -f api
```

| Service | URL |
|---------|-----|
| API | `http://localhost:5000` |
| Swagger UI | `http://localhost:5000/swagger` |
| PostgreSQL | `localhost:5432` |

Untuk menghentikan:
```bash
docker-compose down          # Hentikan container
docker-compose down -v       # Hentikan + hapus data database
```

---

### 2. Manual

#### Step 1 — Setup PostgreSQL

```bash
# Buat database
psql -U postgres -c "CREATE DATABASE siakad_keuangan;"
```

#### Step 2 — Konfigurasi

Edit `appsettings.json` atau buat `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=siakad_keuangan;Username=postgres;Password=YourPassword"
  },
  "MahasiswaApi": {
    "BaseUrl": "https://mahasiswa-api-psi.vercel.app",
    "Endpoint": "/api/mahasiswa"
  }
}
```

#### Step 3 — Jalankan API

```bash
cd SiakadKeuangan.API

dotnet restore
dotnet run
```

API berjalan di `http://localhost:5049` · Swagger UI di `http://localhost:5049/swagger`

> Migrasi database dijalankan **otomatis** saat startup pertama kali.

---

## Konfigurasi

| Key | Default | Keterangan |
|-----|---------|------------|
| `ConnectionStrings:DefaultConnection` | — | Connection string PostgreSQL |
| `MahasiswaApi:BaseUrl` | `https://mahasiswa-api-psi.vercel.app` | Base URL API Mahasiswa eksternal |
| `MahasiswaApi:Endpoint` | `/api/mahasiswa` | Endpoint data mahasiswa |

> Kedua URL API Mahasiswa bersifat **dinamis** — dapat diubah tanpa recompile.

---

## API Endpoints

### Sinkronisasi

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| `POST` | `/api/sinkronisasi/mahasiswa` | Sinkron data mahasiswa dari API eksternal ke database |
| `GET` | `/api/sinkronisasi/preview` | Preview data API Mahasiswa tanpa menyimpan |

### Mahasiswa

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| `GET` | `/api/mahasiswa` | Daftar semua mahasiswa · Query: `?search=nama&prodi=&status=` |
| `GET` | `/api/mahasiswa/{nim}` | Detail mahasiswa berdasarkan ID |

### Tagihan UKT

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| `GET` | `/api/keuangan/tagihan` | Semua tagihan UKT |
| `POST` | `/api/keuangan/tagihan` | Buat tagihan UKT baru |
| `GET` | `/api/keuangan/tagihan/{id}` | Detail tagihan berdasarkan ID |
| `GET` | `/api/keuangan/tagihan/mahasiswa/{nim}` | Tagihan berdasarkan mahasiswa |
| `PATCH` | `/api/keuangan/tagihan/{id}/status` | Update status tagihan manual |

### Pembayaran

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| `POST` | `/api/keuangan/pembayaran` | Catat pembayaran tagihan |
| `GET` | `/api/keuangan/pembayaran` | Semua riwayat pembayaran |
| `GET` | `/api/keuangan/pembayaran/mahasiswa/{nim}` | Riwayat pembayaran per mahasiswa |

### Laporan

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| `GET` | `/api/keuangan/ringkasan/{nim}` | Ringkasan keuangan satu mahasiswa |
| `GET` | `/api/keuangan/dashboard` | Statistik dashboard keseluruhan |

---

### Contoh Request & Response

**POST** `/api/sinkronisasi/mahasiswa`
```json
// Response
{
  "success": true,
  "message": "Sinkronisasi berhasil: 32 data baru, 0 diperbarui",
  "data": {
    "jumlahDataDiambil": 32,
    "jumlahDataBaru": 32,
    "jumlahDataDiupdate": 0,
    "waktuSinkron": "2026-05-30T15:55:01Z"
  }
}
```

**POST** `/api/keuangan/tagihan`
```json
// Request
{
  "nimMahasiswa": "6a0bd56b45d2a2bd6e3078b9",
  "semester": 1,
  "tahunAkademik": 2024,
  "nilaiUkt": 5000000,
  "golonganUkt": "3",
  "jatuhTempo": "2024-09-30T00:00:00Z"
}

// Response
{
  "success": true,
  "message": "Tagihan berhasil dibuat",
  "data": {
    "id": 1,
    "nomorTagihan": "UKT-2024-01-32",
    "statusTagihan": "Belum Bayar",
    "nilaiUkt": 5000000
  }
}
```

**POST** `/api/keuangan/pembayaran`
```json
// Request
{
  "tagihanUktId": 1,
  "jumlahBayar": 5000000,
  "metodePembayaran": "Transfer Bank",
  "keterangan": "Pembayaran UKT Semester 1"
}

// Response
{
  "success": true,
  "message": "Pembayaran berhasil dicatat",
  "data": {
    "nomorTransaksi": "TRX-20240901120000-1",
    "statusPembayaran": "Sukses"
  }
}
```

---

## Database Schema

```
mahasiswa                    tagihan_ukt
─────────────────────        ──────────────────────────
Id           (PK)      ◄──── MahasiswaId   (FK)
Nim          UNIQUE           Id            (PK)
NamaLengkap                  NomorTagihan  UNIQUE
ProgramStudi                 NimMahasiswa
Fakultas                     Semester
Angkatan                     TahunAkademik
StatusAkademik               NilaiUkt
Email                        GolonganUkt
NoHp                         StatusTagihan
CreatedAt                    JatuhTempo
UpdatedAt                    TanggalLunas
                             CreatedAt
                             UpdatedAt
                                  │
                                  ▼
                        riwayat_pembayaran
                        ──────────────────────────
                        Id              (PK)
                        NomorTransaksi  UNIQUE
                        MahasiswaId     (FK)
                        TagihanUktId    (FK)
                        JumlahBayar
                        MetodePembayaran
                        StatusPembayaran
                        Keterangan
                        TanggalBayar
                        CreatedAt

sinkronisasi_log
──────────────────────────
Id
TanggalSinkron
JumlahDataDiambil
JumlahDataBaru
JumlahDataDiupdate
Sukses
PesanError
```

**Status Tagihan:** `Belum Bayar` → `Cicilan` → `Lunas`

**Metode Pembayaran:** `Transfer Bank` · `Virtual Account` · `QRIS` · `Tunai`

---

## Sinkronisasi Data

API Mahasiswa menggunakan MongoDB sehingga field identifier-nya adalah `_id` bukan `nim`. Sistem menangani ini secara otomatis dengan mapping:

```
API Mahasiswa (_id)  →  Database (Nim)
API Mahasiswa (nama) →  Database (NamaLengkap)
```

Proses sinkronisasi bersifat **upsert**:
- Data baru → `INSERT`
- Data sudah ada → `UPDATE`
- Setiap sinkronisasi → dicatat di `sinkronisasi_log`

---

<div align="center">
  <sub>Tugas Mandiri PAA · 2026</sub>
</div>
