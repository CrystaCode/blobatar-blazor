namespace Blobatar;

/// <summary>
/// The three colour slots of a blobatar, as CSS colour strings.
/// </summary>
/// <param name="Background">The plate's fill. Only drawn when a <see cref="BlobatarBackground"/> is set.</param>
/// <param name="Head">The body's fill.</param>
/// <param name="Eye">The eyes' fill.</param>
/// <remarks>
/// Used both ways: as the resolved palette a render paints (every slot filled with <c>#rrggbb</c>), and as
/// <see cref="BlobatarOptions.Palette"/>, where any slot left <see langword="null"/> keeps the resolved colour.
/// Colours passed as overrides bypass the contrast guarantee by definition.
/// </remarks>
public sealed record BlobatarPalette(string? Background = null, string? Head = null, string? Eye = null);
