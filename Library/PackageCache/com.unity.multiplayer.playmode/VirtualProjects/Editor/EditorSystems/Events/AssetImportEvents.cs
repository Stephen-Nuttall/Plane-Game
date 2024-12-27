using System;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class AssetImportEvents
    {
        public event Action<bool, int> RequestImport;
        public void InvokeRequestImport(bool didDomainReload, int numAssetsChanged)
        {
            RequestImport?.Invoke(didDomainReload, numAssetsChanged);
        }
    }
}
