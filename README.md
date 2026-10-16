# EchoLingo ES

Versión en español y más sencilla de instalar, basada en [EchoLingo de codekorvy](https://github.com/codekorvy/echolingo).

> **Estado: preliminar.** La preparación automática de LibreTranslate ya está conectada al botón **Iniciar**. El instalador aún no se ha probado de extremo a extremo en una PC de usuario; consulta [docs/SETUP.md](docs/SETUP.md).

## Objetivo

- Interfaz e instrucciones en español.
- Instalador de Windows, sin instalar el SDK de .NET.
- Preparación de LibreTranslate en un entorno Python aislado al primer uso.
- Aviso de actualizaciones con opciones para actualizar o saltar.
- Subtítulos traducidos localmente para Discord y juegos.

## Estado

- [x] Código base y créditos del proyecto original.
- [x] Comprobador de nuevas versiones y opción para saltarlas.
- [x] Flujo de compilación e instalador de Windows por GitHub Actions.
- [x] Preparación de LibreTranslate desde la app (requiere Python 3.13 con Python Launcher; el instalador puede añadirlo).
- [ ] Probar instalador en Windows 10/11 y juego/Discord.
- [ ] Publicar primera versión estable.

Para compilar desde Windows se requiere .NET 8 SDK. Una etiqueta vX.Y.Z activa las pruebas, compilación y creación de un Release preliminar.

## Créditos y licencias

El código original fue creado por **codekorvy** y se distribuye bajo MIT. Conservamos su licencia y los avisos de terceros: [LICENSE](LICENSE), [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), [UPSTREAM.md](UPSTREAM.md).

LibreTranslate se instala desde PyPI al primer uso y usa AGPL-3.0. No se incrusta en el instalador; la aplicación muestra el aviso de preparación y deja el entorno instalado en el perfil local del usuario.

## Privacidad

El audio se procesa localmente. El comprobador consulta GitHub Releases, no envía grabaciones. El servicio de traducción queda enlazado a 127.0.0.1. No subas modelos descargados ni credenciales al repositorio.
