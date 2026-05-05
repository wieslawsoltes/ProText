namespace ProText.Uno;

/// <summary>
/// Identifies a caret position for Uno presenter navigation APIs.
/// </summary>
public readonly record struct ProTextCharacterHit(int FirstCharacterIndex, int TrailingLength);
