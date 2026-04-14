namespace DeliverTableSharedLibrary.Interfaces;

public interface ITrackable
{
    DateTime Created { get; set; }
    DateTime Updated { get; set; }
}