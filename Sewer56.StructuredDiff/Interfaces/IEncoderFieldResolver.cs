namespace Sewer56.StructuredDiff.Interfaces;

/// <summary>
/// Interface that provides the 'structure' to the encoder,
/// </summary>
public interface IEncoderFieldResolver
{
    /// <summary>
    /// Resolves a field to encode.
    /// </summary>
    /// <param name="offset">Offset of the field in target buffer/array provided to encoder.</param>
    /// <param name="moveBy">How much to move backwards by to get to start of field. Value of 1 means 'move 1 backwards'.</param>
    /// <param name="length">Length of the field.</param>
    /// <returns>True if resolved, false to use unstructured byte-by-byte diff for this address.</returns>
    bool Resolve(nuint offset, out int moveBy, out int length);
}