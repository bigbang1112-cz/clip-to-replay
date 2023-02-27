using GBX.NET;
using GBX.NET.Engines.Game;

namespace ClipToReplay.Tests;

public class ClipToReplayTests
{
    [Fact]
    public async Task Produce_MapData_TMUF()
    {
        var mapData = await File.ReadAllBytesAsync("ClipToReplayTestMap.Challenge.Gbx");
        using var fs = new FileStream("ClipToReplayTestClip.Clip.Gbx", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var clip = GameBox.ParseNode<CGameCtnMediaClip>(fs);
        
        var tool = new ClipToReplayTool(mapData, clip);
        
        var product = (byte[])tool.Produce();

        await File.WriteAllBytesAsync("ClipToReplayTestReplay.Replay.Gbx", product);
    }
    
    [Fact]
    public async Task Produce_MapData_TM2()
    {
        var mapData = await File.ReadAllBytesAsync("ClipToReplayTestMapTM2.Map.Gbx");
        using var fs = new FileStream("ClipToReplayTestClip.Clip.Gbx", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var clip = GameBox.ParseNode<CGameCtnMediaClip>(fs);

        var tool = new ClipToReplayTool(mapData, clip);

        var product = (byte[])tool.Produce();

        await File.WriteAllBytesAsync("ClipToReplayTestReplayTM2.Replay.Gbx", product);
    }
}
