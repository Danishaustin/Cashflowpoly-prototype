# Cashflowpoly-prototype

## Model

### Data

Menangani Model Data yang digunakan saat menarik variabel dari JSON ke dalam bentuk object dan variabel yang dapat digunakan oleh sistem.

### Script

Berisi DataManager, GameState, dan LoginManager

- **DataManager**

  Mengambil dan mengirim data berformat JSON, serta menyimpannya dalam bentuk Dictionary untuk digunakan oleh script / class lainnya.

- **GameState**

  Menyimpan data permainan dan state sementara seperti koin, poin kebahagiaan, investasi yang dimiliki serta hari dan giliran saat ini.

- **LoginManager**

  Kode untuk mengatur autentikasi

## Controller

### Script

Berisi file ChoiceController yang digunakan untuk mengatur input pilihan user. Utamanya berinteraksi dengan DataManager, GameState, dan UIManagerPlay

## View

### UI

Berisi file UXML sebagai struktur dasar UI yang kemudian diatur melalui UIManager.

### Script

berisi kode UIManager yang digunakan pada menu Home dan UIManagerPlay yang digunakan pada Gameplay.

sebagian besar isi script hanya mengatur UI, Animasi, dan Transisi. UIManager memiliki interaksi dengan LoginManager untuk mengatur fitur Login dan akan memiliki interaksi dengan QuestController pada fitur mendatang. UIManagerPlay memiliki interaksi dengan ChoiceController untuk mengatur input pilihan user dan akan memiliki interaksi dengan NarasiController pada fitur mendatang 
