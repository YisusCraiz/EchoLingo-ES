# Instalación y estado actual

## Probar desde el código

Requiere Windows 10 (20H1+) o Windows 11 y .NET 8 SDK.

1. Descarga el repositorio.
2. Ejecuta dotnet test cstranslate.sln en PowerShell.
3. Ejecuta dotnet run --project src/CsTranslate.App.
4. Por ahora, Whisper y un servidor local de LibreTranslate deben estar instalados y ejecutándose.
5. En ajustes, indica el proceso de Discord/juego y las rutas/direcciones necesarias.

## Instalador

Una etiqueta vX.Y.Z inicia el flujo de Windows y produce EchoLingo-ES-Setup.exe como Release preliminar. Incluye la app y el modelo Whisper base, pero aún no instala ni inicia LibreTranslate. La configuración automática de ese servicio es el siguiente hito antes de una versión lista para usuarios.
