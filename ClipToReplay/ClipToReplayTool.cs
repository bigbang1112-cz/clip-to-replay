using GBX.NET;
using GBX.NET.Engines.Game;
using GbxToolAPI;

namespace ClipToReplay;

[ToolName("Clip to Replay")]
[ToolDescription("Joins map and clip together into a replay file.")]
[ToolGitHub("bigbang1112-cz/clip-to-replay")]
public class ClipToReplayTool : ITool, IHasOutput<BinFile>, IConfigurable<ClipToReplayConfig>
{
    private readonly CGameCtnChallenge? map;
    private readonly byte[]? mapData;
    private readonly CGameCtnMediaClip clip;
    
    private static readonly byte[] headerPart1 = new byte[] { 71, 66, 88, 6, 0, 66, 85 };
    private static readonly byte[] headerPart2 = new byte[] { 82, 0, 48, 9, 3 };
    private static readonly byte[] headerPart3 = new byte[] { 69, 0, 0, 0, 0, 0, 0, 0 };

    public ClipToReplayConfig Config { get; set; } = new();

    private static readonly byte[] ghostPlug = {
        20, 48, 9, 3, 9, 0, 0, 0, 2, 0, 0, 0, 1, 0, 0, 0, 0, 32, 9, 3, 24, 32, 9, 3, 3, 0, 0, 0, 0, 0, 0, 64, 10, 0, 0, 0, 83, 116,
        97, 100, 105, 117, 109, 67, 97, 114, 0, 0, 0, 64, 8, 0, 0, 0, 86, 101, 104, 105, 99, 108, 101, 115, 255, 255, 255, 255, 1, 222,
        202, 250, 2, 0, 0, 0, 0, 32, 9, 3, 24, 32, 9, 3, 1, 0, 0, 64, 2, 0, 0, 64, 255, 255, 255, 255, 1, 222, 202, 250, 0, 0, 0, 0, 0, 0, 0, 0
    };
    
    public ClipToReplayTool(CGameCtnChallenge map, CGameCtnMediaClip clip)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.clip = clip ?? throw new ArgumentNullException(nameof(clip));
    }
    
    public ClipToReplayTool(byte[] mapData, CGameCtnMediaClip clip)
    {
        this.mapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
        this.clip = clip ?? throw new ArgumentNullException(nameof(clip));
    }

    public BinFile Produce()
    {
        using var ms = new MemoryStream();
        using var w = new GameBoxWriter(ms);

        w.Write(headerPart1);
        w.Write(Config.Uncompressed ? (byte)85 : (byte)67);
        w.Write(headerPart2);
        w.Write(0);        
        w.Write(headerPart3);

        if (Config.Uncompressed)
        {
            WriteMapAndClip(w);
        }
        else
        {
            WriteCompressedMapAndClip(w);
        }

        return new BinFile(ms.ToArray());
    }

    private void WriteMapAndClip(GameBoxWriter w)
    {
        var md = mapData;
        var m = map;
        
        if (m is null)
        {
            if (md is null)
            {
                throw new Exception("No map data or map provided");
            }

            if (Config.OptimizeMap)
            {
                using var ms = new MemoryStream(md);
                m = GameBox.ParseNode<CGameCtnChallenge>(ms);
            }
        }

        if (m is not null)
        {
            if (Config.OptimizeMap)
            {
                m.HeaderChunks.Remove<CGameCtnChallenge.Chunk03043007>();
            }

            using var ms = new MemoryStream();
            m.Save(ms);
            md = ms.ToArray();
        }

        w.Write(0x03093002);
        w.WriteByteArray(md);

        if (Config.IsTMUF)
        {
            for (var i = 0; i < 2; i++)
            {
                w.State.AuxNodes.Add(i, null);
            }

            (typeof(GbxState).GetProperty(nameof(GbxState.IdVersion)) ?? throw new Exception("Cannot set IdVersion")).SetValue(w.State, 3);

            w.State.IdStrings.Add("StadiumCar");
            w.State.IdStrings.Add("Vehicles");

            w.Write(ghostPlug);
        }

        w.Write(0x03093015);
        w.Write(clip);
        w.Write(0xFACADE01);
    }

    private void WriteCompressedMapAndClip(GameBoxWriter w)
    {
        using var toCompressMs = new MemoryStream();
        using var toCompressW = new GameBoxWriter(toCompressMs);

        WriteMapAndClip(toCompressW);

        var compressed = Lzo.Compress(toCompressMs.ToArray());

        w.Write((uint)toCompressMs.Length);
        w.Write((uint)compressed.Length);
        w.Write(compressed);
    }
}
