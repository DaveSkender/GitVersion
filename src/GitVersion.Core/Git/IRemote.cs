namespace GitVersion.Git;

public interface IRemote : IEquatable<IRemote?>, IComparable<IRemote>
{
    string Name { get; }
    string Url { get; }

    IEnumerable<IRefSpec> FetchRefSpecs { get; }
    IEnumerable<IRefSpec> PushRefSpecs { get; }
}
