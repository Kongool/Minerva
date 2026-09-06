// Layouts FFXIVClientStructs does not expose, taken from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
// Only the rotation interpolation is used here; the surrounding sizes are kept so the offsets stay honest.
using System.Runtime.InteropServices;

namespace Minerva.GameSync;

/// <summary>The local player's movement state, overlaid on its Character. Offsets as of patch 7.3.</summary>
[StructLayout(LayoutKind.Explicit, Size = 0x22E0)]
internal unsafe struct PlayerMove
{
    [FieldOffset(0x1E0)] public MoveContainer Move;
}

[StructLayout(LayoutKind.Explicit, Size = 0x430)]
internal unsafe struct MoveContainer
{
    /// <summary>
    /// The game turns the character over several frames toward <see cref="DesiredRotation"/>. Anything that
    /// sets the facing directly is undone by the next frame of that interpolation unless the desire itself
    /// is rewritten -- which is the whole point of exposing it.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x88)]
    public struct InterpolationState
    {
        [FieldOffset(0x10)] public float DesiredRotation;
        [FieldOffset(0x14)] public float OriginalRotation;
        [FieldOffset(0x40)] public bool RotationInterpolationInProgress;
    }

    [FieldOffset(0x1D0)] public InterpolationState Interpolation;
}
