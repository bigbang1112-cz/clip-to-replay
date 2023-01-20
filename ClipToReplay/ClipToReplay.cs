using GBX.NET.Engines.Game;
using GbxToolAPI;

namespace ClipToReplay;

public class ClipToReplay : Tool, IHasOutput<BinFile>
{
    private readonly CGameCtnChallenge map;
    private readonly CGameCtnMediaClip clip;

    public ClipToReplay(CGameCtnChallenge map, CGameCtnMediaClip clip)
    {
        this.map = map;
        this.clip = clip;
    }

    public BinFile Produce()
    {
        throw new NotImplementedException();
    }
}
