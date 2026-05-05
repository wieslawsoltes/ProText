namespace ProText.MAUI;

/// <summary>
/// Indicates movement direction for presenter caret APIs.
/// </summary>
public enum ProTextLogicalDirection
{
    /// <summary>
    /// Move toward the previous logical character.
    /// </summary>
    Backward,

    /// <summary>
    /// Move toward the next logical character.
    /// </summary>
    Forward,
}

/// <summary>
/// Describes a caret hit in the ProText text stream.
/// </summary>
public readonly record struct ProTextCharacterHit(int FirstCharacterIndex, int TrailingLength);
