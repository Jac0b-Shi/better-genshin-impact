using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Service;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BetterGenshinImpact.GameTask.Common.Element.Assets;

public class MapAssets : BaseAssets<MapAssets>
{
    public Rect MimiMapRect { get; }
    
    public static Rect MimiMapRect1080P =  new Rect(62, 19,212,212);


#if BGI_FULL_WINDOWS
    public MapAssets()
#else
    public static void Initialize(ISystemInfo systemInfo)
    {
        ArgumentNullException.ThrowIfNull(systemInfo);
        if (_instance is not null)
            throw new InvalidOperationException("MapAssets is already initialized. Call DestroyInstance() first.");
        _instance = new MapAssets(systemInfo);
    }

    public new static MapAssets Instance => _instance
        ?? throw new InvalidOperationException("MapAssets.Initialize(...) must be called before Instance.");

    private MapAssets(ISystemInfo systemInfo) : base(systemInfo)
#endif
    {
        MimiMapRect = new Rect((int)Math.Round(62 * AssetScale), (int)Math.Round(19 * AssetScale), (int)Math.Round(212 * AssetScale), (int)Math.Round(212 * AssetScale));
    }
}
