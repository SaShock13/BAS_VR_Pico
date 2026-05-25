public readonly struct SelectionTarget
{
    public readonly SelectionType Type;
    public readonly string PartId;

    public SelectionTarget(SelectionType type, string partId)
    {
        Type = type;
        PartId = partId;
    }
}