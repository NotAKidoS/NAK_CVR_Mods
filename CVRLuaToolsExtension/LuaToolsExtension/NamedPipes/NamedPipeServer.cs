using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using ABI_RC.Core;

namespace NAK.CVRLuaToolsExtension.NamedPipes;

public class NamedPipeServer : Singleton<NamedPipeServer>
{
    private const string PipeName = "UnityPipe";

    public async void StartListening()
    {
        Stopwatch stopwatch = new();
        while (!CommonTools.IsQuitting)
        {
            await using NamedPipeServerStream namedPipeServer = new(PipeName, PipeDirection.In);
            try
            {
                CVRLuaToolsExtensionMod.Logger.Msg("Waiting for client...");
                await namedPipeServer.WaitForConnectionAsync();
                stopwatch.Restart();
                if (!namedPipeServer.IsConnected)
                    continue;

                //Debug.Log("Client connected.");

                // receive and process the ScriptInfo
                ScriptInfo receivedScript = await ReceiveScriptInfo(namedPipeServer);
                ProcessScriptInfo(receivedScript);
            }
            catch (Exception e)
            {
                CVRLuaToolsExtensionMod.Logger.Error(e);
            }

            if (namedPipeServer.IsConnected)
            {
                namedPipeServer.Disconnect();
                CVRLuaToolsExtensionMod.Logger.Msg("Client disconnected.");
            }
            
            stopwatch.Stop();
            CVRLuaToolsExtensionMod.Logger.Msg($"Elapsed time: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        CVRLuaToolsExtensionMod.Logger.Msg("Quitting...");
    }

    private async Task<ScriptInfo> ReceiveScriptInfo(NamedPipeServerStream stream)
    {
        // Read LuaComponentId (4 bytes for an int)
        byte[] intBuffer = new byte[4];
        await stream.ReadAsync(intBuffer, 0, intBuffer.Length);
        int luaComponentId = BitConverter.ToInt32(intBuffer, 0);

        string assetId = await ReadStringAsync(stream);
        //CVRLuaToolsExtensionMod.Logger.Msg($"Received assetId: {assetId}");
        
        string scriptName = await ReadStringAsync(stream);
        //CVRLuaToolsExtensionMod.Logger.Msg($"Received scriptName: {scriptName}");
        
        string scriptPath = await ReadStringAsync(stream);
        //CVRLuaToolsExtensionMod.Logger.Msg($"Received scriptPath: {scriptPath}");
        
        string scriptText = await ReadStringAsync(stream);
        //CVRLuaToolsExtensionMod.Logger.Msg($"Received scriptText: {scriptText}");
        
        //Debug.Log("ScriptInfo received.");
        
        return new ScriptInfo
        {
            LuaComponentId = luaComponentId,
            AssetId = assetId,
            ScriptName = scriptName,
            ScriptPath = scriptPath,
            ScriptText = scriptText
        };
    }

    private static async Task<string> ReadStringAsync(Stream stream)
    {
        // Define a buffer size and initialize a memory stream
        byte[] buffer = new byte[1024];
        using MemoryStream memoryStream = new();

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            memoryStream.Write(buffer, 0, bytesRead);
            //CVRLuaToolsExtensionMod.Logger.Msg($"Read {bytesRead} bytes.");
            if (bytesRead < buffer.Length) break; // Assuming no partial writes
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
    
    private static void ProcessScriptInfo(ScriptInfo scriptInfo)
    {
        // Handle the received ScriptInfo data
        //Debug.Log($"Received ScriptInfo: {scriptInfo.AssetId}, {scriptInfo.LuaComponentId}, {scriptInfo.ScriptName}");
        
        LuaHotReloadManager.OnReceiveUpdatedScript(scriptInfo);
    }
}
