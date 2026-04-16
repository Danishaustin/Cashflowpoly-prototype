# Cashflowpoly-prototype

## Model

### Data

berisi Model Data yang digunakan saat menarik variabel dari JSON ke dalam data variabel C# Unity.

### Script

berisi DataManager, GameState, dan LoginManager

- DataManager

  berisi kode untuk mengambil dan mengirim data berformat JSON, serta menyimpannya dalam bentuk Dictionary untuk digunakan oleh script / class lainnya.

- GameState

  kode untuk menyimpan data permainan sementara seperti koin, poin kebahagiaan, investasi yang dimiliki serta hari dan giliran saat ini.

- LoginManager

  kode untuk mengatur autentikasi

## Controller

### Script

berisi file ChoiceController yang digunakan untuk mengatur input pilihan user. Utamanya berinteraksi dengan DataManager, GameState, dan UIManagerPlay

## View

### UI

berisi kode uxml yang digunakan sebagai rancangan UI kemudian diatur melalui UIManager.

### Script

berisi kode UIManager yang digunakan pada menu Home dan UIManagerPlay yang digunakan pada Gameplay.

sebagian besar isi script hanya mengatur UI, Animasi, dan Transisi. UIManager memiliki interaksi dengan LoginManager untuk mengatur fitur Login dan akan memiliki interaksi dengan QuestController pada fitur mendatang. UIManagerPlay memiliki interaksi dengan ChoiceController untuk mengatur input pilihan user dan akan memiliki interaksi dengan NarasiController pada fitur mendatang 
