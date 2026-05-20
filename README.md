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

Berisi file UIManager yang digunakan pada menu Home dan UIManagerPlay yang digunakan pada Gameplay.

sebagian besar isi script hanya mengatur UI, Animasi, dan Transisi. 

- UIManager

  Memiliki interaksi dengan LoginManager untuk mengatur fitur Login dan akan memiliki interaksi dengan QuestController pada fitur mendatang.
  
- UIManagerPlay

  Memiliki interaksi dengan ChoiceController untuk mengatur input pilihan user dan akan memiliki interaksi dengan NarasiController pada fitur mendatang

## Narasi

### Script

- NarasiData

  NarasiData.cs akan bertugas sebagai template format narasi.json yang akan dimasukkan ke dalam Dictionary pada DataManager. Isi dari narasi.json meliputi:

  - "Aksi"

    berupa nama Aksi yang membutuhkan narasinya.
    
  - "AksiKe"
 
    Digunakan untuk mengidentifikasi pada "Aksi" ke-berapa narasi akan ditampilkan. Script akan membaca 0 sebagai narasi default yang akan ditampilkan setiap melakukan "Aksi".
    
  - "Narasi1":

    Narasi dipisahkan menjadi 3 bagian utama dan "Narasi1" adalah narasi yang akan dimunculkan pertama.
    
  - "Narasi2"
 
    Narasi yang akan dimunculkan setelah "Narasi1".
    
  - "Narasi3"
 
    Narasi yang akan dimunculkan setelah "Narasi2".

- NarasiController

  NarasiController utamanya digunakan untuk InitiateNarasi dan HandleNarasi yang akan dipanggil dari ChoiceController. InitiateNarasi dipanggil dari UIManager, sedangkan HandleNarasi dipanggil dari ChoiceController setiap kali Aksi dilakukan

  - InitiateNarasi
 
    Fungsi yang berinteraksi dengan DataManager untuk mengambil data narasi berdasarkan Aksi dan AksiKe yang akan disimpan ke dalam Dictionary lokal.

  - HandleNarasi

    Fungsi yang akan berinteraksi dengan UIManager untuk menampilkan narasi secara bertahap .
