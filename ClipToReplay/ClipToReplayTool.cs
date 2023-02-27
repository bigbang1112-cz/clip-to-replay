﻿using GBX.NET;
using GBX.NET.Engines.Game;
using GbxToolAPI;

namespace ClipToReplay;

public class ClipToReplayTool : Tool, IHasOutput<BinFile>
{
    private readonly CGameCtnChallenge? map;
    private readonly byte[]? mapData;
    private readonly CGameCtnMediaClip clip;
    
    private static readonly byte[] headerPart1 = new byte[] { 71, 66, 88, 6, 0, 66, 85 };
    private static readonly byte[] headerPart2 = new byte[] { 82, 0, 224, 7, 36 };
    private static readonly byte[] headerPart3 = new byte[] { 69, 0, 0, 0, 0, 0, 0, 0 };

    private static readonly byte[] ghostPlug = {
        20, 48, 9, 3, 9, 0, 0, 0, 2, 0, 0, 0, 1, 0, 0, 0, 0, 32, 9, 3, 24, 32, 9, 3, 3, 0, 0, 0, 0, 0, 0, 64, 10, 0, 0, 0, 83, 116,
        97, 100, 105, 117, 109, 67, 97, 114, 0, 0, 0, 64, 8, 0, 0, 0, 86, 101, 104, 105, 99, 108, 101, 115, 255, 255, 255, 255, 1, 222,
        202, 250, 2, 0, 0, 0, 0, 32, 9, 3, 24, 32, 9, 3, 1, 0, 0, 64, 2, 0, 0, 64, 255, 255, 255, 255, 1, 222, 202, 250, 0, 0, 0, 0, 0, 0, 0, 0
    };

    public bool Uncompressed { get; set; }
    
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
        w.Write(Uncompressed ? (byte)85 : (byte)67);
        w.Write(headerPart2);
        w.Write(0);
        w.Write(headerPart3);

        if (Uncompressed)
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

        for (var i = 0; i < 2; i++)
        {
            w.State.AuxNodes.Add(i, null);
            w.State.IdStrings.Add("StadiumCar");
            w.State.IdStrings.Add("Vehicles");
        }

        w.Write(0x03093002);
        w.Write(md.Length);
        w.Write(md);
        w.Write(ghostPlug);
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