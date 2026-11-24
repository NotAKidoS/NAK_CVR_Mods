using System.Collections;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using StbImageSharp;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.CleanPlates;

internal static class NameplateIcons
{
    private class IconState
    {
        public string LoadedUrl;
        public Texture2D Texture;
        public bool Loading;
        public bool Alive = true;
    }

    private static readonly Dictionary<PlayerBase, IconState> States = new();

    // Longest side of plate image
    private const int MaxSize = 256;

    public static void Fetch(PlayerBase player, string url)
    {
        if (!States.TryGetValue(player, out IconState state))
            States[player] = state = new IconState();

        if (state.Loading) return;
        if (state.LoadedUrl == url)
        {
            // Already downloaded, but the plate may have been cleared since
            // (the image setting was toggled, or the style was swapped).
            if (state.Texture != null)
                PlateManager.UpdateData(player, d => d.Icon = state.Texture);
            return;
        }

        state.Loading = true;
        ImageQueueSystem.Instance.AddCoroutine(GetPlayerImage(player, url, state));
    }

    private static IEnumerator GetPlayerImage(PlayerBase player, string url, IconState state)
    {
        var task = Task.Run(() => LoadMips(url));
        while (!task.IsCompleted) yield return null;

        // Reading Result on a faulted task throws, and this runs inside the
        // game's shared image queue coroutine, which would take the queue with it.
        (byte[][] mips, int width, int height) = task.IsFaulted || task.IsCanceled ? default : task.Result;
        Texture2D texture = mips != null ? Upload(mips, width, height) : null;

        state.Loading = false;

        // Player left while downloading
        if (!state.Alive)
        {
            if (texture != null) Object.Destroy(texture);
            yield break;
        }

        if (texture == null)
        {
            CleanPlatesMod.Logger.Warning($"Error downloading nameplate image for {player.PlayerUsername} from: {url}");
            state.LoadedUrl = url;
            yield break;
        }

        if (state.Texture != null)
            Object.Destroy(state.Texture);

        state.Texture = texture;
        state.LoadedUrl = url;

        PlateManager.UpdateData(player, d => d.Icon = texture);
    }

    // Decode and shrink off main thread (game loader builds full mip chain anyway)
    private static async Task<(byte[][] mips, int width, int height)> LoadMips(string url)
    {
        byte[] bytes = await ImageCache.GetImageBytesAsync(url);
        ImageResult image = bytes != null ? TextureLoader.Decode(bytes) : null;
        if (image == null) return default;

        byte[][] mips = TextureLoader.BuildMips(image.Data, image.Width, image.Height);
        int skip = 0;
        while (skip < mips.Length - 1 && Math.Max(image.Width, image.Height) >> skip > MaxSize) skip++;
        return (mips[skip..], Math.Max(1, image.Width >> skip), Math.Max(1, image.Height >> skip));
    }

    private static Texture2D Upload(byte[][] mips, int width, int height)
    {
        Texture2D texture = new(width, height, TextureFormat.RGBA32, mips.Length, false);
        for (int mip = 0; mip < mips.Length; mip++) texture.SetPixelData(mips[mip], mip);

        // DXT needs 4x4 blocks, else RGBA32
        bool compress = (width & 3) == 0 && (height & 3) == 0;
        texture.Apply(false, !compress);
        if (compress)
        {
            texture.Compress(false);
            texture.Apply(false, true);
        }
        return texture;
    }

    public static void Release(PlayerBase player)
    {
        if (!States.Remove(player, out IconState state))
            return;
        state.Alive = false; // in-flight coroutine discards its result
        if (state.Texture != null)
            Object.Destroy(state.Texture);
    }
}