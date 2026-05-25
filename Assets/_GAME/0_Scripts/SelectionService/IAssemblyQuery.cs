public interface IAssemblyQuery
{
    PartDomainState GetPartDomainState(string id);
    DroneDomainState  GetDroneDomainState(string id);
}
