# Instalación y estado actual

## Probar desde el código

Requiere Windows 10 (20H1+) o Windows 11, .NET 8 SDK y Python 3.13 con el Python Launcher.

1. Descarga el repositorio.
2. Ejecuta `dotnet test cstranslate.sln` en PowerShell.
3. Ejecuta `dotnet run --project src/CsTranslate.App`.
4. En la app, selecciona Discord o el juego y pulsa **Iniciar**.
5. Si LibreTranslate no está disponible, confirma la preparación automática. EchoLingo crea un entorno Python aislado en `%LOCALAPPDATA%\EchoLingo ES\LibreTranslate`, instala LibreTranslate y levanta el servidor solo en `127.0.0.1`. No cambia la instalación global de Python.

La primera preparación requiere internet y puede tardar varios minutos. Descarga los paquetes del motor y los modelos de traducción inglés/español. Al cerrar EchoLingo, el proceso del servidor se detiene; el entorno y los modelos quedan guardados para el siguiente uso. Si falta Python 3.13, instálalo desde [python.org](https://www.python.org/downloads/) y vuelve a iniciar.

## Instalador

Una etiqueta vX.Y.Z inicia el flujo de Windows y produce EchoLingo-ES-Setup.exe como Release preliminar. Incluye la app, el modelo Whisper base y el instalador oficial de Python 3.13. Durante la instalación puedes dejar marcada la casilla recomendada para instalar Python solo para tu usuario, o desmarcarla y añadirlo después. LibreTranslate se prepara al primer uso, tras confirmarlo en la app.

## Validación del motor de traducción

El flujo [Validate LibreTranslate on Windows](../.github/workflows/windows-libretranslate.yml) comprueba en una máquina limpia de GitHub Actions que la versión fijada de LibreTranslate se instala con Python 3.12 y 3.13, carga inglés/español y completa una traducción de prueba. En Windows se fuerza UTF-8 y el servicio escucha en loopback.

La validación de CI confirma la ruta de instalación del motor. Falta probar de extremo a extremo el instalador, la captura de audio de una aplicación real y el overlay en Windows 10/11.
