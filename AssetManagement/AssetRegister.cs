using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.AssetManagement
{
    public static class AssetServer
    {
        private static Dictionary<string, object> assets = new Dictionary<string, object>();

        public static T RequestOrLoad<T>(string assetPath)
        {
            if (assets.ContainsKey(assetPath)) { return (T)assets[assetPath]; }

            T asset = MGame.Instance.Content.Load<T>(assetPath);

            assets.Add(assetPath, asset);
            return asset;
        }
    }
}
