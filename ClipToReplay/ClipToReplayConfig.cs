using GbxToolAPI;

namespace ClipToReplay;

public class ClipToReplayConfig : Config
{
    public bool Uncompressed { get; set; }
    public bool IsTMUF { get; set; }
    public bool OptimizeMap { get; set; } = true;
}
