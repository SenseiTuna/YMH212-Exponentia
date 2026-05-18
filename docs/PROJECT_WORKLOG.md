# Exponentia Project Worklog

Bu dokuman, projede yapilan degisikliklerin kisa ve izlenebilir kaydidir.
Her anlamli kod degisikliginden sonra guncellenmelidir.

## Kayit Formati

- Tarih:
- Is Paketi:
- Degisen Dosyalar:
- Yapilanlar:
- Dogrulama:
- Not:

---

## 2026-05-03 - HUD Iskeleti ve Debug Panel

- Tarih: 2026-05-03
- Is Paketi: HUD iskeleti ve debug panel kurulumu
- Degisen Dosyalar:
  - `Exponentia_Game/Assets/Scripts/UI/PlayerHudController.cs`
  - `Exponentia_Game/Assets/Scripts/UI/DebugPanelController.cs`
  - `Exponentia_Game/Assets/Tests/EditMode/HudAndDebugPanelTests.cs`
- Yapilanlar:
  - Oyuncu can, mana, skill, silah ve temel bilgi alanlarini guncelleyen HUD iskeleti eklendi.
  - `F3` ile acilip kapanan, FPS/pozisyon/kaynak/input bilgisi gosteren debug panel controller eklendi.
  - HUD ve debug metin formatini kontrol eden unit testler eklendi.
- Dogrulama:
  - `HudAndDebugPanelTests` 3/3 Passed
  - Rapor: `Exponentia_Game/TestResults_20260503_203729.xml`
- Not:
  - Testler arayuzde PlayMode altinda da gorunebilir; bu davranis assembly yapisina gore degisebilir.

---

## 2026-05-03 - SampleScene F3 Duzeltmesi

- Tarih: 2026-05-03
- Is Paketi: Debug panelin sahne bagimsiz calismasi
- Degisen Dosyalar:
  - `Exponentia_Game/Assets/Scripts/UI/DebugPanelController.cs`
- Yapilanlar:
  - `DebugPanelBootstrap` eklendi; sahne yuklenince otomatik `DebugPanelController` olusturuluyor.
  - `DebugPanelController` icine runtime UI fallback eklendi; panel veya text yoksa otomatik Canvas/Panel/Text uretiliyor.
  - Boylece `SampleScene` gibi UI kurulumu olmayan sahnelerde de `F3` ile panel ac/kapa calisiyor.
- Dogrulama:
  - Lint: ilgili dosyada hata yok.
- Not:
  - Sahneye manuel debug panel koyulursa bootstrap ikinci bir panel olusturmaz.

---

## 2026-05-03 - Debug Panel Ekrana Sigdirma

- Tarih: 2026-05-03
- Is Paketi: Debug panel gorunum iyilestirme
- Degisen Dosyalar:
  - `Exponentia_Game/Assets/Scripts/UI/DebugPanelController.cs`
- Yapilanlar:
  - Debug panel boyutu ekran cozunurlugune gore oranli ayarlanir hale getirildi.
  - Konum `Screen.safeArea` dikkate alinacak sekilde sol-ust guvenli bolgeye sabitlendi.
  - Ekran boyutu degisince panel otomatik yeniden fit ediliyor.
  - Font boyutu bir tik buyutulerek okunurluk artirildi.
- Dogrulama:
  - Lint: ilgili dosyada hata yok.
- Not:
  - F3 ile acildiginda panel yeniden fit edilir; kirpilma problemi azaltilmistir.

---

## PlayerCharacterApplier NullReference (pasif prefab)

- Tarih: 2026-05-03
- Is Paketi: Spawn sirasinda karakter uygulama hatasi
- Degisen Dosyalar:
  - `Exponentia_Game/Assets/Scripts/Player/PlayerCharacterApplier.cs`
  - `Exponentia_Game/Assets/Scripts/SceneFlow/PlayerSpawner.cs`
- Yapilanlar:
  - `PlayerBase` prefab kokunun pasif olmasi durumunda `Awake` calismadan `ApplyCharacter` cagrilabiliyordu; `spriteRenderer` null kalip `ApplyVisual` patliyordu.
  - `PlayerSpawner`: spawn sonrasi kok `SetActive(true)` ile aktifleniyor (Awake garantisi).
  - `PlayerCharacterApplier`: `ResolveReferences` ile `Awake`/`ApplyCharacter` icinde referans cozumu; `SpriteRenderer` yoksa cocuklarda aranıyor ve null icin guvenli cikis + hata logu.
  - `ApplyStats` icin `PlayerStats` null kontrolu.
- Dogrulama:
  - Lint: ilgili dosyalarda hata yok.
- Not:
  - Uzun vadede prefab kokunu editorden aktif yapmak da iyi pratik.

---

## 2026-05-03 - Debug Panel Pozisyon Ayari

- Tarih: 2026-05-03
- Is Paketi: Debug panel konumunu sabitleme
- Degisen Dosyalar:
  - `Exponentia_Game/Assets/Scripts/UI/DebugPanelController.cs`
- Yapilanlar:
  - Panel konumu inspector'dan ayarlanabilir `panelPosition` alanina tasindi.
  - Varsayilan konum kullanici talebine gore `x: 90`, `y: -50` olarak ayarlandi.
  - Safe area clamp korunarak panelin ekran disina tasmasi engellendi.
- Dogrulama:
  - Lint: ilgili dosyada hata yok.
- Not:
  - Inspector uzerinden `panelPosition` degistirilerek istendigi gibi ince ayar yapilabilir.

---

## 2026-05-04 - Character Scene Debug Panel Acilis Duzeltmesi

- Tarih: 2026-05-04
- Is Paketi: Character sahnesinden baslangicta debug panelin gorunmemesi
- Degisen Dosyalar:
  - `Exponentia_Game/Assets/Scripts/UI/DebugPanelController.cs`
- Yapilanlar:
  - `DebugPanelController` icindeki bozulmus kosul bloklari duzeltildi (`player` bulunamama kontrolu ve erken cikis akisi).
  - `DebugPanelBootstrap` metodu tekrar gecerli hale getirildi; `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` ile otomatik olusum geri kazanildi.
  - Otomatik olusturulan debug panelin sahneler arasi korunmasi icin `DontDestroyOnLoad` eklendi.
  - Runtime olusturulan durumda panelin varsayilan olarak acik gelmesi icin `showOnStart = true` yapildi.
- Dogrulama:
  - Kod incelemesi: derleme akisini bozan sentaks hatalari temizlendi, bootstrap tetiklenebilir hale geldi.
- Not:
  - Sahneye manuel `DebugPanelController` ekli ise bootstrap ikinci bir kopya olusturmaz.
