namespace NAK.CVRLuaToolsExtension;

[Serializable]
public struct ScriptInfo
{
    public string AssetId; // we will reload all scripts with this asset id
    public int LuaComponentId; // the target lua component id
    
    public string ScriptName; // the new script name
    public string ScriptPath;
    public string ScriptText; // the new script text
}