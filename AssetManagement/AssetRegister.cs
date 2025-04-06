using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.AssetManagement
{
    public static class AssetServer
    {
        private static ConcurrentDictionary<string, object> assets = new ConcurrentDictionary<string, object>();

        public static T RequestOrLoad<T>(string assetPath)
        {
            if (assets.ContainsKey(assetPath)) { return (T)assets[assetPath]; }

            T asset = MGame.Instance.Content.Load<T>(assetPath);

            assets.TryAdd(assetPath, asset);
            return asset;
        }
    }
}
