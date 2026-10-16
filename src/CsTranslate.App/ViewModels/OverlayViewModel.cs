using System.Collections.ObjectModel;
using CsTranslate.Core.Models;

namespace CsTranslate.App.ViewModels;

public sealed class OverlayViewModel
{
    private readonly int _maxLines;

    public ObservableCollection<TranslatedLine> Lines { get; } = [];

    public OverlayViewModel(int maxLines = 6)
    {
        _maxLines = maxLines;
    }

    /// <summary>Must be called on the UI thread (the pipeline marshals via Dispatcher).</summary>
    public void AddLine(TranslatedLine line)
    {
        Lines.Add(line);
        while (Lines.Count > _maxLines)
        {
            Lines.RemoveAt(0);
        }
    }
}
