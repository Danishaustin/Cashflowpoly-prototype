# Database Cashflowpoly

Dokumen ini menjelaskan data yang saat ini dipakai project dan rekomendasi struktur jika data dipindahkan dari JSON, PlayerPrefs, dan memory runtime ke database.

## Ringkasan

Saat ini project menyimpan data dalam 3 bentuk:

- **JSON di `Assets/Resources/Data`**: data master seperti bahan, resep, kebutuhan, target kebutuhan, narasi, dan quest.
- **PlayerPrefs**: konfigurasi game baru seperti jumlah pemain dan nama tiap player.
- **Memory runtime di `GameState`**: coin, happiness, saving, inventory, day, turn, dan progress permainan.

Jika memakai database, pisahkan data menjadi:

- **Master Data**: data statis yang jarang berubah.
- **Game Session Data**: data satu permainan berjalan.
- **Player Session Data**: data tiap player dalam satu permainan.
- **Progress Data**: inventory, quest progress, dan counter aksi.

## Master Data

Master data adalah data referensi yang bisa dikelola dari admin/editor dan diload saat game mulai.

### `bahan_makanan`

Berasal dari `bahan.json` dan `BahanMakananData.cs`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik, bisa sama dengan nama tanpa spasi |
| `nama` | string | Nama bahan |
| `harga_beli` | int | Harga beli bahan |

Contoh:

```json
{
  "id": "Nasi",
  "nama": "Nasi",
  "harga_beli": 2
}
```

### `resep`

Berasal dari `resep.json` dan `ResepData.cs`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik resep |
| `nama` | string | Nama masakan |
| `harga_jual` | int | Coin yang didapat saat dijual |
| `poin_kebahagiaan` | int | Happiness yang didapat |

Karena satu resep punya banyak bahan, bahan resep sebaiknya dipisah.

### `resep_bahan`

| Field | Type | Keterangan |
|---|---|---|
| `resep_id` | string | Relasi ke `resep.id` |
| `bahan_id` | string | Relasi ke `bahan_makanan.id` |
| `jumlah` | int | Jumlah bahan yang dibutuhkan |

Contoh:

```json
{
  "resep_id": "NasiGoreng",
  "bahan_id": "Nasi",
  "jumlah": 1
}
```

### `kebutuhan`

Berasal dari `kebutuhan.json` dan `KebutuhanData.cs`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik kebutuhan |
| `nama` | string | Nama kebutuhan |
| `tipe` | string | Kategori kebutuhan |

### `target_kebutuhan`

Berasal dari `targetKebutuhan.json` dan `TargetKebutuhanData.cs`.

Target kebutuhan adalah kumpulan 3 syarat kebutuhan yang harus dipenuhi player. Setiap syarat bisa mengacu ke tipe kebutuhan, misalnya `primer`, atau nama kebutuhan spesifik, misalnya `Boneka`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik target kebutuhan |
| `nama` | string | Nama target kebutuhan |
| `poin_kebahagiaan_berhasil` | int | Happiness yang ditambahkan jika target berhasil |
| `poin_kebahagiaan_gagal` | int | Happiness yang dikurangi jika target gagal, disimpan sebagai nilai negatif |

Karena satu target kebutuhan selalu memiliki 3 item target, detail target sebaiknya dipisah.

### `target_kebutuhan_items`

| Field | Type | Keterangan |
|---|---|---|
| `target_kebutuhan_id` | string | Relasi ke `target_kebutuhan.id` |
| `urutan` | int | Urutan target, 1 sampai 3 |
| `jenis_target` | string | `tipe` atau `nama` |
| `value` | string | Nilai target, misalnya `primer`, `sekunder`, atau `Boneka` |

Contoh:

```json
{
  "target_kebutuhan_id": "target_primer_sekunder_boneka",
  "items": [
    {
      "urutan": 1,
      "jenis_target": "tipe",
      "value": "primer"
    },
    {
      "urutan": 2,
      "jenis_target": "tipe",
      "value": "sekunder"
    },
    {
      "urutan": 3,
      "jenis_target": "nama",
      "value": "Boneka"
    }
  ]
}
```

### `tujuan_finansial`

Berasal dari `tujuanFinansial.json` dan `TujuanFinansialData.cs`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik tujuan |
| `nama` | string | Nama tujuan finansial |
| `harga_beli` | int | Harga pembelian dari tabungan |
| `poin_kebahagiaan` | int | Happiness yang didapat |

### `narasi`

Berasal dari `narasi.json` dan `NarasiData.cs`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik narasi, saat ini field `nama` |
| `narasi_1` | string | Teks dialog tahap 1 |
| `narasi_2` | string | Teks dialog tahap 2 |
| `narasi_3` | string | Teks dialog tahap 3 |

Narasi tidak lagi menyimpan `aksi` dan `aksi_ke` sebagai field utama. Trigger narasi dan syarat tambahan disimpan di `prerequisiteAksi`.

### `narasi_prerequisite_aksi`

| Field | Type | Keterangan |
|---|---|---|
| `narasi_id` | string | Relasi ke `narasi.id` |
| `aksi` | string | Aksi yang menjadi syarat, misalnya `JualMasakan` |
| `value` | int | Jumlah aksi yang menjadi trigger atau jumlah minimal aksi untuk syarat tambahan |

Aturan penggunaan:

- Jika `value = 0`, narasi menjadi default untuk aksi tersebut dan bisa dijalankan setiap kali aksi itu dilakukan.
- Jika `value > 0` pada aksi yang sedang dijalankan, narasi menjadi narasi khusus untuk aksi ke-`value`.
- Jika ada aksi lain dalam list yang sama, aksi tersebut menjadi syarat tambahan dan harus sudah dilakukan minimal sebanyak `value`.

Contoh:

```json
{
  "narasi_id": "kebutuhan_setelah_jual_dua",
  "prerequisite_aksi": [
    {
      "aksi": "Kebutuhan",
      "value": 3
    },
    {
      "aksi": "JualMasakan",
      "value": 2
    }
  ]
}
```

Artinya narasi aktif saat aksi `Kebutuhan` dilakukan untuk ke-3 kalinya, dengan syarat `JualMasakan` sudah dilakukan minimal 2 kali.

Contoh narasi default:

```json
{
  "aksi": "Kebutuhan",
  "value": 0
}
```

Artinya narasi tersebut bisa menjadi fallback setiap kali aksi `Kebutuhan` dijalankan. Tidak ada lagi penanda `TidakAda`; aksi yang menjadi default tetap ditulis secara eksplisit.

### `quest`

Berasal dari `quest.json` dan `QuestData.cs`.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string | Key unik quest |
| `title` | string | Judul quest |
| `description` | string | Deskripsi quest |
| `aksi` | string | Aksi yang menambah progress |
| `target` | int | Target progress |

Contoh:

```json
{
  "id": "jual_3_masakan",
  "title": "Pedagang Baru",
  "description": "Jual 3 masakan kepada pembeli.",
  "aksi": "JualMasakan",
  "target": 3
}
```

## Game Session Data

Game session adalah satu permainan baru dari Home sampai game selesai.

### `game_sessions`

| Field | Type | Keterangan |
|---|---|---|
| `id` | string/guid | ID unik permainan |
| `player_count` | int | Jumlah player, 3 atau 4 |
| `day` | int | Hari saat ini |
| `turn` | int | Player yang sedang aktif |
| `moves_left` | int | Sisa aksi player aktif |
| `finish_day` | int | Batas akhir permainan |
| `is_game_over` | bool | Status selesai |
| `created_at` | datetime | Waktu game dibuat |
| `updated_at` | datetime | Waktu update terakhir |

Data ini menggantikan sebagian field runtime di `GameState`.

## Player Session Data

### `session_players`

Menyimpan data tiap player dalam satu game session.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string/guid | ID unik player dalam session |
| `session_id` | string/guid | Relasi ke `game_sessions.id` |
| `player_index` | int | Nomor player, 1 sampai 4 |
| `name` | string | Nama player dari Home |
| `coins` | int | Coin player, initial 20 |
| `happiness` | int | Happiness player, initial 0 |
| `saving` | int | Tabungan player, initial 0 |

Contoh initial state:

```json
{
  "session_id": "session_001",
  "player_index": 1,
  "name": "Doni",
  "coins": 20,
  "happiness": 0,
  "saving": 0
}
```

## Inventory Dan Kepemilikan

Saat ini inventory bahan dan kebutuhan ada di `GameState`. Jika tiap player punya inventory sendiri, simpan dengan `session_player_id`.

### `player_bahan`

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `bahan_id` | string | Relasi ke `bahan_makanan.id` |
| `jumlah` | int | Jumlah bahan yang dimiliki |

### `player_kebutuhan`

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `kebutuhan_id` | string | Relasi ke `kebutuhan.id` |
| `tipe` | string | Tipe kebutuhan saat dibeli |

## Target Kebutuhan Progress

Jika target kebutuhan diberikan per player setiap game baru, simpan target aktif dan status penyelesaiannya di session player.

### `player_target_kebutuhan`

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `target_kebutuhan_id` | string | Relasi ke `target_kebutuhan.id` |
| `is_completed` | bool | Apakah semua item target sudah terpenuhi |
| `is_failed` | bool | Apakah target gagal dipenuhi |
| `reward_applied` | bool | Apakah efek happiness sudah diberikan |

Saat evaluasi target dilakukan, sistem perlu mengecek `player_kebutuhan` milik player:

- Target dengan `jenis_target = tipe` terpenuhi jika player punya minimal 1 kebutuhan dengan tipe tersebut.
- Target dengan `jenis_target = nama` terpenuhi jika player punya kebutuhan dengan nama tersebut.
- Setiap item target dihitung sebagai 1 syarat. Contoh target `(primer, sekunder, Boneka)` berarti player butuh 1 barang tipe primer, 1 barang tipe sekunder, dan 1 barang bernama Boneka.

Contoh:

```json
{
  "session_player_id": "player_001",
  "target_kebutuhan_id": "target_primer_sekunder_boneka",
  "is_completed": false,
  "is_failed": false,
  "reward_applied": false
}
```

### `player_tujuan_finansial`

Jika tujuan finansial bisa dimiliki/dibeli per player.

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `tujuan_finansial_id` | string | Relasi ke `tujuan_finansial.id` |
| `purchased_at_day` | int | Hari saat dibeli |

## Quest Progress

Jika quest dibuat per player setiap game baru, progress tidak perlu permanen di master data. Simpan progress pada session player.

### `player_quest_progress`

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `quest_id` | string | Relasi ke `quest.id` |
| `progress` | int | Progress saat ini |
| `target` | int | Target saat quest dimulai |
| `is_completed` | bool | Apakah quest selesai |
| `is_reward_claimed` | bool | Apakah reward sudah diberikan |

Contoh:

```json
{
  "session_player_id": "player_001",
  "quest_id": "jual_3_masakan",
  "progress": 2,
  "target": 3,
  "is_completed": false,
  "is_reward_claimed": false
}
```

## Counter Aksi Player

Saat ini counter aksi narasi ada di `GameState`:

- `jmAksiKe`
- `tfAksiKe`
- `bmAksiKe`
- `klAksiKe`
- `kAksiKe`

Jika narasi dan quest perlu dihitung per player, simpan dalam tabel umum.

### `player_action_counters`

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `aksi` | string | Nama aksi |
| `count` | int | Jumlah aksi yang sudah dilakukan |

Contoh:

```json
{
  "session_player_id": "player_001",
  "aksi": "JualMasakan",
  "count": 2
}
```

## Peduli Donasi

Saat ini `GameState` memiliki:

```csharp
public Dictionary<int, List<int>> JuaraPeduliDonasi { get; private set; }
```

Struktur ini menyimpan urutan juara donasi setiap Peduli Donasi berhasil dilakukan.

- Key `int`: urutan event Peduli Donasi, misalnya `1`, `2`, `3`.
- Value `List<int>`: daftar nomor player yang sudah diurutkan dari total donasi terbesar.

Contoh:

```json
{
  "1": [2, 1, 3],
  "2": [2, 3, 1]
}
```

Artinya:

- Pada event Peduli Donasi ke-1, urutan juara adalah Player 2, Player 1, Player 3.
- Pada event Peduli Donasi ke-2, urutan juara adalah Player 2, Player 3, Player 1.

Karena ranking ini dihitung dari total akumulasi donasi tiap player, database sebaiknya menyimpan dua jenis data: total donasi player dan snapshot ranking tiap event.

### `player_peduli_donasi`

Menyimpan total akumulasi donasi tiap player dalam satu game session.

| Field | Type | Keterangan |
|---|---|---|
| `session_player_id` | string/guid | Relasi ke `session_players.id` |
| `total_donasi` | int | Total coin yang pernah didonasikan player |

Contoh:

```json
{
  "session_player_id": "player_002",
  "total_donasi": 12
}
```

### `peduli_donasi_events`

Menyimpan setiap event Peduli Donasi yang terjadi.

| Field | Type | Keterangan |
|---|---|---|
| `id` | string/guid | ID unik event donasi |
| `session_id` | string/guid | Relasi ke `game_sessions.id` |
| `event_ke` | int | Urutan event donasi |
| `day` | int | Hari saat donasi dilakukan |

### `peduli_donasi_rankings`

Menyimpan snapshot ranking pada setiap event Peduli Donasi.

| Field | Type | Keterangan |
|---|---|---|
| `event_id` | string/guid | Relasi ke `peduli_donasi_events.id` |
| `rank` | int | Urutan ranking, mulai dari 1 |
| `session_player_id` | string/guid | Player pada ranking tersebut |
| `total_donasi` | int | Total donasi player saat snapshot dibuat |

Contoh:

```json
[
  {
    "event_id": "donasi_event_001",
    "rank": 1,
    "session_player_id": "player_002",
    "total_donasi": 12
  },
  {
    "event_id": "donasi_event_001",
    "rank": 2,
    "session_player_id": "player_001",
    "total_donasi": 8
  }
]
```

## PlayerPrefs Yang Sekarang Dipakai

Saat ini data setup game baru disimpan sementara di `PlayerPrefs`.

| Key | Type | Keterangan |
|---|---|---|
| `PlayerCount` | int | Jumlah player, 3 atau 4 |
| `PlayerName_1` | string | Nama player 1 |
| `PlayerName_2` | string | Nama player 2 |
| `PlayerName_3` | string | Nama player 3 |
| `PlayerName_4` | string | Nama player 4, hanya jika player count 4 |

Jika memakai database, data ini menjadi bagian dari `game_sessions` dan `session_players`.

## Relasi Singkat

```text
game_sessions
  -> session_players
      -> player_bahan
      -> player_kebutuhan
      -> player_target_kebutuhan
      -> player_tujuan_finansial
      -> player_quest_progress
      -> player_action_counters
      -> player_peduli_donasi
  -> peduli_donasi_events
      -> peduli_donasi_rankings

quest
  -> player_quest_progress

target_kebutuhan
  -> target_kebutuhan_items
  -> player_target_kebutuhan

resep
  -> resep_bahan
      -> bahan_makanan

narasi
  -> narasi_prerequisite_aksi
  -> matched by aksi + value
```
