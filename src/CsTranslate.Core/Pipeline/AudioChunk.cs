using CsTranslate.Audio;

namespace CsTranslate.Core.Pipeline;

public sealed class AudioChunk
{
    public required byte[] Pcm { get; init; }
    public required WaveFormatInfo Format { get; init; }
}
