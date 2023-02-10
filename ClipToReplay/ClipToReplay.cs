using GBX.NET;
using GBX.NET.Engines.Game;
using GbxToolAPI;

namespace ClipToReplay;

public class ClipToReplay : Tool<ClipToReplayConfig>, IHasOutput<BinFile>
{
    private readonly CGameCtnChallenge? map;
    private readonly byte[]? mapData;
    private readonly CGameCtnMediaClip clip;
    
    private static readonly byte[] headerPart1 = new byte[] { 71, 66, 88, 6, 0, 66, 85 };
    private static readonly byte[] headerPart2 = new byte[] { 82, 0, 224, 7, 36, 0, 0, 0, 0, 0, 0, 0, 0 };

    public override ClipToReplayConfig Config { get; set; } = new();

    public ClipToReplay(CGameCtnChallenge map, CGameCtnMediaClip clip)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.clip = clip ?? throw new ArgumentNullException(nameof(clip));
    }
    
    public ClipToReplay(byte[] mapData, CGameCtnMediaClip clip)
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

        if (md is null)
        {
            if (map is null)
            {
                throw new Exception("Map data or map object missing");
            }

            using var ms = new MemoryStream();
            map.Save(ms);

            md = ms.ToArray();
        }
        
        w.Write(0x03093002);
        w.Write(md.Length);
        w.Write(md);
        w.Write(0x03093015); // could maybe use 0x0309300C
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
