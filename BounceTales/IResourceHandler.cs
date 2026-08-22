using BounceTales.Ext.Rsc;

namespace BounceTales;

public interface IResourceHandler
{
    bool LoadResidentData(DataInputStream dis, int type);
    object ReadResource(DataInputStream dis, int readLength, ResourceType type, int resBatchId);
    bool LoadResource(DataInputStream dis, string finalRscPath, int readLength, ResourceType resType, int batchId, int subResIdx);
    bool UnloadResource(ResourceType resType, int unloadResId);
}
