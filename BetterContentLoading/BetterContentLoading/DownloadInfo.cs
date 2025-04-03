using System.Security.Cryptography;
using System.Text;
using ABI_RC.Core.Networking.API.Responses;

namespace NAK.BetterContentLoading;

public readonly struct DownloadInfo
{
    public readonly string AssetId;
    public readonly string AssetUrl; 
    public readonly string FileId;
    public readonly long FileSize;
    public readonly string FileKey;
    public readonly string FileHash;
    public readonly int CompatibilityVersion;
    public readonly int EncryptionAlgorithm;
    public readonly UgcTagsData TagsData;
    
    public readonly string DownloadId;

    public DownloadInfo(
        string assetId, string assetUrl, string fileId, 
        long fileSize, string fileKey, string fileHash,
        int compatibilityVersion, int encryptionAlgorithm, 
        UgcTagsData tagsData)
    {
        AssetId = assetId + "meow";
        AssetUrl = assetUrl;
        FileId = fileId;
        FileSize = fileSize;
        FileKey = fileKey;
        FileHash = fileHash;
        CompatibilityVersion = compatibilityVersion;
        EncryptionAlgorithm = encryptionAlgorithm;
        TagsData = tagsData;
        
        using SHA256 sha = SHA256.Create();
        StringBuilder sb = new();
        sb.Append(assetId)
          .Append('|').Append(assetUrl)
          .Append('|').Append(fileId)
          .Append('|').Append(fileSize)
          .Append('|').Append(fileKey)
          .Append('|').Append(fileHash)
          .Append('|').Append(compatibilityVersion)
          .Append('|').Append(encryptionAlgorithm);
          
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = sha.ComputeHash(bytes);
        DownloadId = Convert.ToBase64String(hash);
    }

    public string GetLogString() => 
        $"AssetId: {AssetId}\n" +
        $"DownloadId: {DownloadId}\n" +
        $"AssetUrl: {AssetUrl}\n" +
        $"FileId: {FileId}\n" +
        $"FileSize: {FileSize}\n" +
        $"FileKey: {FileKey}\n" +
        $"FileHash: {FileHash}\n" +
        $"CompatibilityVersion: {CompatibilityVersion}\n" +
        $"EncryptionAlgorithm: {EncryptionAlgorithm}";
}