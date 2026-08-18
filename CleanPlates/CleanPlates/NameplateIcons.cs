using System.Collections;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
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
        Task<Texture2D> task = Task.Run(() => ImageCache.GetImageAsync(url));
        while (!task.IsCompleted) yield return null;

        // Reading Result on a faulted task throws, and this runs inside the
        // game's shared image queue coroutine, which would take the queue with it.
        Texture2D texture = task.IsFaulted || task.IsCanceled ? null : task.Result;

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

    public static void Release(PlayerBase player)
    {
        if (!States.Remove(player, out IconState state))
            return;
        state.Alive = false; // in-flight coroutine discards its result
        if (state.Texture != null)
            Object.Destroy(state.Texture);
    }
}