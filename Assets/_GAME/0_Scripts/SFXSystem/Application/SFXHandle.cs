using System;

public readonly struct SFXHandle : IEquatable<SFXHandle>
{
    internal readonly long Value;
    internal SFXHandle(long value) { Value = value; }
    public bool IsValid => Value != 0;
    public bool Equals(SFXHandle other) => Value == other.Value;
    public override bool Equals(object obj) => obj is SFXHandle other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

