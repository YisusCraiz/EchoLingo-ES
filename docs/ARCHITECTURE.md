# Mimari

## Veri akışı

```
[Hedef uygulama / oyun process'i]
        |  WASAPI Process Loopback (Win10 20H1+)
        v
CsTranslate.Audio    -- ProcessLoopbackCapture: ham PCM yakalama (16-bit, 44.1kHz, stereo)
        |               fallback: WasapiSystemLoopbackCapture (NAudio, tum sistem sesi)
        v
CsTranslate.Core      -- VoiceActivitySegmenter: ~2sn (yumusak) / 6sn (sert tavan) parcalara bolme
  Pipeline/               TranslationPipeline: Channel<T> tabanli asenkron orkestrasyon
        v
CsTranslate.Speech    -- WhisperTranscriber: Whisper.net (whisper.cpp), lokal GGML model
        v
CsTranslate.Cleanup   -- GlossaryCleanupService: JSON sozluk/regex tabanli duzeltme (LLM YOK)
        v
CsTranslate.Translation -- LibreTranslateClient: lokal LibreTranslate sunucusuna HTTP
        v
CsTranslate.App        -- OverlayViewModel -> OverlayWindow (WPF, her zaman ustte, tiklama gecirgen)
```

## Neden bu tasarım

- **100% lokal/ucretsiz**: STT (Whisper.net), temizleme (kural tabanli) ve ceviri (LibreTranslate) hicbiri bulut API'si gerektirmez.
- **Process-ozel ses yakalama**: Windows'un sundugu tek yol olan `ActivateAudioInterfaceAsync` + `AUDIOCLIENT_ACTIVATION_PARAMS` (process loopback) API'si icin hazir bir NuGet paketi yok; `Microsoft.Windows.CsWin32` kaynak ureticisi kullanilarak Microsoft'un resmi win32 metadata'sindan dogru P/Invoke imzalari otomatik uretildi (bkz. `src/CsTranslate.Audio/NativeMethods.txt`).
- **16-bit PCM 44.1kHz format secimi**: Microsoft'un resmi `ApplicationLoopback` ornegiyle birebir ayni format ve `AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM` bayragi kullanildi. Bu bayrak olmadan, istenen format ses motorunun ic mix formatiyla eslesmezse HRESULT hatasi degil, anlamsiz/gurultu veri elde ediliyor (gelistirme sirasinda dogrudan gozlemlendi).
- **Channel<T> pipeline**: Ses yakalama asla bloklanmamali (DropOldest), STT/temizleme/ceviri asamalari ise bekleyebilir (bounded/unbounded Wait modu). Her asama kendi Task'inda calisir.
- **Kural tabanli temizleme**: LLM promptuna gerek kalmadi cunku deterministik bir sozluk/regex katmani hem yeterli hem de tam birim test edilebilir (bkz. `tests/CsTranslate.Cleanup.Tests`).

## Proje yapisi

`src/CsTranslate.{Core,Audio,Speech,Cleanup,Translation,App}`, `tests/`, `tools/`, `docs/`.

## Bilinen sinirlamalar

- LibreTranslate/Argos Translate kalitesi ticari motorlardan (DeepL, Google) daha dusuk; oyun jargonu (`bi` = B site gibi kisaltmalar) genelde cevrilmeden kaliyor. `CsTranslate.Cleanup/Rules/*.json` dosyalarina yeni kurallar eklenerek zamanla iyilestirilebilir.
- `ggml-base` modeli CPU'da gercek zamanliya yakin calisir ama 2-6 saniyelik VAD parcalarinin sinirlarinda kelimeler bazen yarim kalabilir; `ChunkSoftSeconds`/`ChunkHardCapSeconds` ayarlariyla dengelenebilir.
