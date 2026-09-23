using UnityEngine;

namespace CCP
{

/// <summary>
/// Extension methods for Unity's Vector3 type.
/// </summary>
public static class Vector3Extensions
{
    /// <summary>
    /// Returns a new Vector3 with the specified Z position while keeping X and Y unchanged.
    /// </summary>
    /// <param name="position">The source Vector3.</param>
    /// <param name="zPosition">The new Z position.</param>
    /// <returns>A new Vector3 with updated Z position.</returns>
    public static Vector3 WithZ(this Vector3 position, float zPosition)
    {
        return new Vector3(position.x, position.y, zPosition);
    }
}

}