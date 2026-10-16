# EchoLingo ES

Versión en español y más sencilla de instalar, basada en [EchoLingo de codekorvy](https://github.com/codekorvy/echolingo).

> **Estado: preliminar.** El instalador aún no está publicado. La primera versión instala la app y el modelo Whisper. La configuración automática de LibreTranslate sigue pendiente; consulta [docs/SETUP.md](docs/SETUP.md).

## Objetivo

- Interfaz e instrucciones en español.
- Instalador de Windows, sin instalar el SDK de .NET.
- Preparación automática de modelos y servicio de traducción.
- Aviso de actualizaciones con opciones para actualizar o saltar.
- Subtítulos de traducción local para Discord y juegos.

## Estado

- [x] Código base y créditos del proyecto original.
- [x] Comprobador de nuevas versiones y opción para saltarlas.
- [x] Flujo de compilación e instalador de Windows por GitHub Actions.
- [ ] Preparar automáticamente LibreTranslate y sus modelos.
- [ ] Probar instalador en Windows 10/11 y juego/Discord.
- [ ] Publicar primera versión estable.

Para compilar desde Windows se requiere .NET 8 SDK. Una etiqueta vX.Y.Z activa las pruebas, compilación y creación de un Release preliminar.

## Créditos y licencias

El código original fue creado por **codekorvy** y se distribuye bajo MIT. Conservamos su licencia y los avisos de terceros: [LICENSE](LICENSE), [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), [UPSTREAM.md](UPSTREAM.md).

LibreTranslate usa AGPL-3.0 y no se incluye en el código ni en el instalador inicial.

## Privacidad

El audio se procesa localmente. El comprobador consulta GitHub Releases, no envía grabaciones. No subas modelos descargados ni credenciales al repositorio.
