namespace ProText.Core;

/// <summary>
/// Framework-neutral editable text composition helpers for presenter-style controls.
/// </summary>
public static class ProTextEditableText
{
    public static ProTextEditableTextSnapshot CreateSnapshot(ProTextEditableTextOptions options)
    {
        var sourceText = options.Text ?? string.Empty;
        var caretIndex = Math.Clamp(options.CaretIndex, 0, sourceText.Length);
        var preeditText = string.IsNullOrEmpty(options.PreeditText) ? null : options.PreeditText;
        var passwordMasked = options.PasswordChar != default && !options.RevealPassword;
        var displayText = passwordMasked ? new string(options.PasswordChar, sourceText.Length) : sourceText;
        var displayPreeditText = passwordMasked && preeditText is not null
            ? new string(options.PasswordChar, preeditText.Length)
            : preeditText;
        var preeditCursorOffset = GetPreeditCursorOffset(preeditText, options.PreeditTextCursorPosition);

        return new ProTextEditableTextSnapshot(
            sourceText,
            displayText,
            displayPreeditText,
            caretIndex,
            caretIndex + preeditCursorOffset);
    }

    public static ProTextRichContent CreateContent(
        ProTextEditableTextOptions options,
        ProTextRichStyle baseStyle,
        ProTextRichStyle? preeditStyle)
    {
        var snapshot = CreateSnapshot(options);
        var builder = new ProTextRichContentBuilder(baseStyle);

        if (!string.IsNullOrEmpty(snapshot.DisplayPreeditText))
        {
            builder.AppendText(snapshot.DisplayText[..snapshot.CaretIndex], baseStyle);
            builder.AppendText(snapshot.DisplayPreeditText, preeditStyle ?? baseStyle);
            builder.AppendText(snapshot.DisplayText[snapshot.CaretIndex..], baseStyle);
            return builder.Build();
        }

        builder.AppendText(snapshot.DisplayText, baseStyle);
        return builder.Build();
    }

    public static int GetEffectiveCaretIndex(ProTextEditableTextOptions options)
    {
        return CreateSnapshot(options).EffectiveCaretIndex;
    }

    private static int GetPreeditCursorOffset(string? preeditText, int? cursorPosition)
    {
        if (string.IsNullOrEmpty(preeditText))
        {
            return 0;
        }

        return cursorPosition is >= 0
            ? Math.Min(cursorPosition.Value, preeditText.Length)
            : preeditText.Length;
    }
}

public readonly record struct ProTextEditableTextOptions(
    string? Text,
    int CaretIndex,
    string? PreeditText,
    int? PreeditTextCursorPosition,
    char PasswordChar,
    bool RevealPassword);

public readonly record struct ProTextEditableTextSnapshot(
    string SourceText,
    string DisplayText,
    string? DisplayPreeditText,
    int CaretIndex,
    int EffectiveCaretIndex);
