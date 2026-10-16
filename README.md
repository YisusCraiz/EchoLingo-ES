# EchoLingo ES

Versión en español y más sencilla de instalar, basada en [EchoLingo de codekorvy](https://github.com/codekorvy/echolingo).

> Proyecto en desarrollo. Aún no hay instalador publicado. La aplicación traduce el audio de una aplicación de Windows a subtítulos en pantalla. Esta primera etapa prepara la base del código y la compilación automática para Windows.

## Objetivo

- Interfaz e instrucciones en español.
- Instalador de Windows y descarga inicial guiada de modelos.
- Subtítulos traducidos en Discord y juegos.
- Aviso de nuevas versiones con opciones para actualizar o saltar.
- Procesamiento local del audio.

## Estado

- [x] Repositorio propio y créditos de origen.
- [ ] Instalador probado en Windows.
- [ ] Configuración automática de Whisper y LibreTranslate.
- [ ] Actualizador con descarga e instalación desde Releases.
- [ ] Prueba en Discord y juegos.
- [ ] Primera versión pública.

## Compilación

El proyecto usa .NET 8, WPF y Windows 10/11. Consulta [docs/SETUP.md](docs/SETUP.md) para los requisitos actuales. Los Releases se compilarán con GitHub Actions al publicar una etiqueta `v*`.

## Créditos y licencias

El código original de EchoLingo fue creado por **codekorvy** y se distribuye bajo MIT. Conservamos su licencia y los avisos de terceros; consulta [LICENSE](LICENSE) y [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

LibreTranslate es un servicio separado con licencia AGPL-3.0. No se incluye en este repositorio. No se publicará un instalador hasta revisar los avisos y condiciones de cada componente empaquetado.

## Privacidad

El objetivo es que el audio se procese localmente. El comprobador de actualizaciones consultará GitHub Releases para detectar nuevas versiones. No se deben subir grabaciones ni modelos descargados al repositorio.
