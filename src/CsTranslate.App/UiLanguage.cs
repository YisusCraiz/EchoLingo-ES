using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CsTranslate.App;

/// <summary>Interface language only; speech and translation language settings are independent.</summary>
internal static class UiLanguage
{
    private static volatile string _language = "es";
    private static readonly Dictionary<string, string> English = new()
    {
        ["Escucha. Comprende. Sigue la conversación."] = "Listen. Understand. Stay in the conversation.",
        ["_Traducir"] = "_Translate",
        ["01  Audio de la conversación"] = "01  Conversation audio",
        ["Buscar apps"] = "Find apps",
        ["Reproduce una voz en la aplicación y actualiza la lista."] = "Play speech in the app, then refresh this list.",
        ["Reproduce una voz en WhatsApp, Discord o tu juego. Después busca y selecciona esa aplicación."] = "Play speech in WhatsApp, Discord or your game. Then find and select that app.",
        ["Aplicación cuyo audio se traducirá"] = "App whose audio will be translated",
        ["SEÑAL DE AUDIO"] = "AUDIO SIGNAL",
        ["Nivel del audio capturado"] = "Captured audio level",
        ["Inicia la captura para comprobar la señal."] = "Start capture to check the audio signal.",
        ["02  Idiomas"] = "02  Languages",
        ["Elige qué idioma aceptar y a cuál traducir. Con «Detectar idioma» se aceptan ambos."] = "Choose which spoken language to accept and which language to translate into. Detect language accepts both.",
        ["Se escucha"] = "Spoken language",
        ["Idioma hablado"] = "Spoken language",
        ["Detectar idioma"] = "Detect language",
        ["Inglés"] = "English",
        ["Español"] = "Spanish",
        ["Quiero leer"] = "Translate into",
        ["Idioma de los subtítulos"] = "Subtitle language",
        ["03  Tus subtítulos"] = "03  Your subtitles",
        ["F9 abre el historial: usa la rueda para leer mensajes anteriores. Pulsa F9 otra vez para seguir en vivo. En juegos, usa pantalla completa sin bordes."] = "Press F9 to browse earlier messages with the mouse wheel. Press F9 again to return to live subtitles. Use borderless fullscreen in games.",
        ["Mostrar y colocar subtítulos"] = "Show and position subtitles",
        ["_Ajustes"] = "_Settings",
        ["Configuración avanzada"] = "Advanced settings",
        ["Los valores predeterminados funcionan con la instalación habitual. Cambia estos campos si necesitas una configuración personalizada."] = "The default values work with a standard installation. Change these fields only if you need a custom setup.",
        ["Proceso de la aplicación (selección manual)"] = "App process (manual selection)",
        ["Nombre .exe o PID. Se completa al seleccionar una aplicación."] = "Executable name or PID. Filled in when you select an app.",
        ["Modelo de voz Whisper (.bin)"] = "Whisper speech model (.bin)",
        ["Elegir archivo del modelo"] = "Choose model file",
        ["Dirección del servidor de traducción"] = "Translation server address",
        ["Códigos de idioma personalizados"] = "Custom language codes",
        ["Idioma destino (ejemplo: es)"] = "Target language (example: es)",
        ["Idioma hablado (vacío = automático)"] = "Spoken language (empty = automatic)",
        ["Busca una aplicación con audio para comenzar."] = "Find an app playing audio to get started.",
        ["_Iniciar traducción"] = "_Start translation",
        ["_Detener"] = "S_top",
        ["Salir"] = "Exit",
        ["EchoLingo ES - Subtítulos"] = "EchoLingo ES - Subtitles",
        ["Usa la rueda para leer el historial. Arrastra esta barra para mover el recuadro. F9 vuelve a los subtítulos en vivo."] = "Scroll to read earlier messages. Drag this bar to move the overlay. Press F9 to return to live subtitles.",
        ["Aquí aparecerán los subtítulos. Inicia la captura y reproduce una voz. F9 permite mover y cambiar el tamaño."] = "Subtitles will appear here. Start capture and play speech. Press F9 to move and resize this overlay.",
        ["Idioma de la interfaz"] = "Interface language",
        ["Buscando aplicaciones que estén reproduciendo audio..."] = "Finding apps that are playing audio...",
        ["No detecté audio activo. Reproduce una voz o sonido y pulsa «Buscar audio»."] = "No active audio found. Play speech or sound and click Find apps.",
        ["Fuente detectada: {0}. Pulsa Iniciar para capturarla."] = "Source found: {0}. Click Start translation to capture it.",
        ["Detecté {0} aplicaciones con audio. Selecciona la que quieres capturar."] = "Found {0} apps with audio. Select the one you want to capture.",
        ["No se pudieron detectar las aplicaciones de audio: {0}"] = "Could not find audio apps: {0}",
        ["Fuente seleccionada: {0}. Pulsa Iniciar subtítulos."] = "Source selected: {0}. Click Start translation.",
        ["Fuente seleccionada: {0}. Pulsa Detener y después Iniciar para cambiar."] = "Source selected: {0}. Click Stop, then Start translation to switch.",
        ["Selecciona el modelo de Whisper"] = "Select a Whisper model",
        ["Modelos Whisper (*.bin)|*.bin|Todos los archivos (*.*)|*.*"] = "Whisper models (*.bin)|*.bin|All files (*.*)|*.*",
        ["Inicia los subtítulos para medir el audio."] = "Start translation to measure the audio.",
        ["Se está recibiendo audio. El nivel indica sonido, no necesariamente voz."] = "Audio is being received. The level indicates sound, not necessarily speech.",
        ["Sin señal reciente: reproduce una voz y comprueba que elegiste la aplicación correcta."] = "No recent signal: play speech and check that you selected the right app.",
        ["Captura iniciada. Esperando audio de la aplicación..."] = "Capture started. Waiting for audio from the app...",
        ["No se pudo iniciar: {0}"] = "Could not start: {0}",
        ["No se encontró una aplicación en ejecución llamada '{0}'."] = "No running app named '{0}' was found.",
        ["No se encontró el archivo del modelo Whisper: {0}"] = "Whisper model file not found: {0}",
        ["EchoLingo puede preparar LibreTranslate en un entorno aislado dentro de tu usuario. Se necesita Python 3.13 y conexión a internet. La primera descarga de paquetes y modelos puede tardar varios minutos y ocupar bastante espacio. ¿Continuar?"] = "EchoLingo can set up LibreTranslate in an isolated environment in your user profile. Python 3.13 and internet access are required. The first package and model downloads may take several minutes and use significant disk space. Continue?",
        ["Preparar traducción"] = "Set up translation",
        ["Preparación cancelada."] = "Setup cancelled.",
        ["Preparando el traductor..."] = "Setting up the translator...",
        ["No se pudo preparar LibreTranslate: {0}"] = "Could not set up LibreTranslate: {0}",
        ["Iniciando captura de audio..."] = "Starting audio capture...",
        ["Traduciendo audio de {0} -> {1}."] = "Translating audio from {0} -> {1}.",
        ["Error: {0}"] = "Error: {0}",
        ["Subtítulos detenidos."] = "Subtitles stopped.",
        ["Cerrando EchoLingo..."] = "Closing EchoLingo...",
        ["La preparación automática solo funciona con una dirección local HTTP, por ejemplo http://localhost:5000. Para una dirección remota, configura ese servidor por separado."] = "Automatic setup only works with a local HTTP address, such as http://localhost:5000. Set up remote servers separately.",
        ["Buscando Python 3.13..."] = "Looking for Python 3.13...",
        ["Creando un entorno aislado..."] = "Creating an isolated environment...",
        ["Descargando e instalando LibreTranslate. La primera descarga puede tardar..."] = "Downloading and installing LibreTranslate. The first download may take a while...",
        ["Windows no pudo iniciar LibreTranslate."] = "Windows could not start LibreTranslate.",
        ["Iniciando el traductor y preparando inglés/español..."] = "Starting the translator and preparing English/Spanish...",
        ["LibreTranslate se cerró durante el inicio. Revisa el registro: {0}"] = "LibreTranslate closed during startup. Check the log: {0}",
        ["LibreTranslate no respondió después de 10 minutos. Revisa el registro: {0}"] = "LibreTranslate did not respond within 10 minutes. Check the log: {0}",
        ["No se pudo iniciar {0}."] = "Could not start {0}.",
        ["No se encontró Python 3.13. Vuelve a ejecutar el instalador de EchoLingo y marca “Instalar Python 3.13”, o instálalo desde python.org y vuelve a intentar."] = "Python 3.13 was not found. Run the EchoLingo installer again and select Install Python 3.13, or install it from python.org and try again.",
        ["{0} terminó con código {1}."] = "{0} exited with code {1}.",
        ["Hay una nueva versión de EchoLingo ES ("] = "A new version of EchoLingo ES is available (",
        ["Sí: abrir la página para descargarla.\nNo: saltar esta versión."] = "Yes: open the download page.\nNo: skip this version.",
        ["Actualización disponible"] = "Update available",
        ["Ocurrió un error inesperado:\n\n"] = "An unexpected error occurred:\n\n",
        ["\n\nConsulta el registro de errores para más detalles."] = "\n\nCheck the error log for more details.",
        ["Continuar"] = "Continue",
        ["Cancelar"] = "Cancel",
        ["Descargar actualización"] = "Download update",
        ["Saltar versión"] = "Skip version",
        ["No se pudo guardar el idioma: {0}"] = "Could not save the interface language: {0}",
        ["Deteniendo..."] = "Stopping...",
    };

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["Ui_0"] = "Escucha. Comprende. Sigue la conversación.",
        ["Ui_1"] = "_Traducir",
        ["Ui_2"] = "01  Audio de la conversación",
        ["Ui_3"] = "Buscar apps",
        ["Ui_4"] = "Reproduce una voz en la aplicación y actualiza la lista.",
        ["Ui_5"] = "Reproduce una voz en WhatsApp, Discord o tu juego. Después busca y selecciona esa aplicación.",
        ["Ui_6"] = "Aplicación cuyo audio se traducirá",
        ["Ui_7"] = "SEÑAL DE AUDIO",
        ["Ui_8"] = "Nivel del audio capturado",
        ["Ui_9"] = "Inicia la captura para comprobar la señal.",
        ["Ui_10"] = "02  Idiomas",
        ["Ui_11"] = "Elige qué idioma aceptar y a cuál traducir. Con «Detectar idioma» se aceptan ambos.",
        ["Ui_12"] = "Se escucha",
        ["Ui_13"] = "Idioma hablado",
        ["Ui_14"] = "Detectar idioma",
        ["Ui_15"] = "Inglés",
        ["Ui_16"] = "Español",
        ["Ui_17"] = "Quiero leer",
        ["Ui_18"] = "Idioma de los subtítulos",
        ["Ui_19"] = "03  Tus subtítulos",
        ["Ui_20"] = "F9 abre el historial: usa la rueda para leer mensajes anteriores. Pulsa F9 otra vez para seguir en vivo. En juegos, usa pantalla completa sin bordes.",
        ["Ui_21"] = "Mostrar y colocar subtítulos",
        ["Ui_22"] = "_Ajustes",
        ["Ui_23"] = "Configuración avanzada",
        ["Ui_24"] = "Los valores predeterminados funcionan con la instalación habitual. Cambia estos campos si necesitas una configuración personalizada.",
        ["Ui_25"] = "Proceso de la aplicación (selección manual)",
        ["Ui_26"] = "Nombre .exe o PID. Se completa al seleccionar una aplicación.",
        ["Ui_27"] = "Modelo de voz Whisper (.bin)",
        ["Ui_28"] = "Elegir archivo del modelo",
        ["Ui_29"] = "Dirección del servidor de traducción",
        ["Ui_30"] = "Códigos de idioma personalizados",
        ["Ui_31"] = "Idioma destino (ejemplo: es)",
        ["Ui_32"] = "Idioma hablado (vacío = automático)",
        ["Ui_33"] = "Busca una aplicación con audio para comenzar.",
        ["Ui_34"] = "_Iniciar traducción",
        ["Ui_35"] = "_Detener",
        ["Ui_36"] = "Salir",
        ["Ui_37"] = "EchoLingo ES - Subtítulos",
        ["Ui_38"] = "Usa la rueda para leer el historial. Arrastra esta barra para mover el recuadro. F9 vuelve a los subtítulos en vivo.",
        ["Ui_39"] = "Aquí aparecerán los subtítulos. Inicia la captura y reproduce una voz. F9 permite mover y cambiar el tamaño.",
        ["Ui_40"] = "Idioma de la interfaz",
    };

    public static string T(string spanish) =>
        _language == "en" && English.TryGetValue(spanish, out string? english) ? english : spanish;

    public static string F(string template, params object?[] args) =>
        string.Format(_language == "en" ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("es-MX"),
            T(template), args);

    public static void SetLanguage(string? language)
    {
        _language = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "es";
        foreach (var label in Labels)
        {
            Application.Current.Resources[label.Key] = T(label.Value);
        }
    }

    public static bool Confirm(string message, string title, string accept, string cancel)
    {
        var dialog = new Window
        {
            Title = title,
            Width = Math.Min(460, SystemParameters.WorkArea.Width),
            SizeToContent = SizeToContent.Height,
            MaxHeight = SystemParameters.WorkArea.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (owner is not null) dialog.Owner = owner;
        else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var layout = new DockPanel { Margin = new Thickness(20) };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var yes = new Button { Content = T(accept), IsDefault = true, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 12, 8, 0) };
        var no = new Button { Content = T(cancel), IsCancel = true, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 12, 0, 0) };
        yes.Click += (_, _) => dialog.DialogResult = true;
        no.Click += (_, _) => dialog.DialogResult = false;
        actions.Children.Add(yes);
        actions.Children.Add(no);
        DockPanel.SetDock(actions, Dock.Bottom);
        layout.Children.Add(actions);
        layout.Children.Add(new ScrollViewer
        {
            MaxHeight = Math.Max(100, SystemParameters.WorkArea.Height - 160),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14 }
        });
        dialog.Content = layout;
        return dialog.ShowDialog() == true;
    }
}
