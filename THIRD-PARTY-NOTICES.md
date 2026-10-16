# Avisos de terceros y créditos

## Código original

Este proyecto se basa en [EchoLingo](https://github.com/codekorvy/echolingo), creado por **codekorvy**. El código original y nuestras modificaciones están bajo MIT. Se conserva el texto original en [LICENSE](LICENSE).

## Dependencias

| Dependencia | Licencia | Uso |
|---|---|---|
| NAudio | MIT | Captura de audio |
| Whisper.net | MIT | Reconocimiento de voz |
| whisper.cpp | MIT | Motor nativo usado por Whisper.net |
| OpenAI Whisper | MIT | Modelo de reconocimiento de voz |
| Microsoft.Windows.CsWin32 | MIT | Generación de interop durante compilación |

Cada licencia y aviso correspondiente debe acompañar a las distribuciones.

## Componente externo

[LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) usa AGPL-3.0. Es un servidor local separado. Su código y modelos no se incluyen en este repositorio ni en el instalador inicial. Antes de empaquetarlo se revisarán sus requisitos de redistribución.

El instalador se compila con Inno Setup en GitHub Actions; no se incluye el ejecutable de Inno Setup.
