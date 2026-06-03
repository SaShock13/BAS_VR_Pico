using System.Collections.Generic;

public interface IDroneCollisionAnalyzer
{
    IReadOnlyList<string> FindCollisions(
        DroneDomainState drone);
}