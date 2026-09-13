using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SummonMastery;

internal static class Dismissal
{
    private const string Request = Plugin.Id + ".DismissAll.v1";
    private const string Result = Plugin.Id + ".DismissResult.v1";
    private static readonly System.Reflection.MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");
    private static ZRoutedRpc registered;
    private static float nextRequest;
    private static readonly Dictionary<long, float> LastRequest = new();

    internal static void Update()
    {
        var rpc = ZRoutedRpc.instance;
        if (rpc == null) return;
        if (!ReferenceEquals(registered, rpc))
        {
            rpc.Register(Request, DismissOnServer);
            rpc.Register<int>(Result, ShowResult);
            registered = rpc;
            LastRequest.Clear();
            nextRequest = 0;
        }
        var player = Player.m_localPlayer;
        if (!player || !Hud.instance || !Plugin.DismissKey.Value.IsDown() || Time.unscaledTime < nextRequest) return;
        if (!(bool)TakeInput.Invoke(player, null)) return;
        nextRequest = Time.unscaledTime + 1f;
        // The no-peer overload routes to the server. No caller-supplied player ID or object IDs.
        rpc.InvokeRoutedRPC(Request);
    }

    private static void DismissOnServer(long sender)
    {
        if (!Plugin.Instance || !ZNet.instance || !ZNet.instance.IsServer() || ZDOMan.instance == null) return;
        long playerID;
        if (sender == ZDOMan.GetSessionID())
        {
            var player = Player.m_localPlayer;
            if (!player || player.IsDead()) return;
            playerID = player.GetPlayerID();
        }
        else
        {
            var peer = ZNet.instance.GetPeer(sender);
            if (peer == null) return;
            var playerData = ZDOMan.instance.GetZDO(peer.m_characterID);
            if (playerData == null || playerData.GetOwner() != sender) return;
            playerID = playerData.GetLong(ZDOVars.s_playerID);
        }
        if (playerID == 0) return;
        if (LastRequest.TryGetValue(sender, out var last) && Time.unscaledTime - last < 1f) return;
        LastRequest[sender] = Time.unscaledTime;
        int count = 0;
        // The server has world records even for unloaded sectors. Enumerate a snapshot before deletion.
        var ids = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long,
            (Plugin.Key + "summoner").GetStableHashCode());
        foreach (var id in ids)
        {
            var data = ZDOMan.instance.GetZDO(id);
            if (data == null || !data.IsValid() || !Scaling.CanDismiss(Plugin.Tagged(data),
                data.GetLong(Plugin.Key + "summoner"), playerID, data.GetFloat(ZDOVars.s_health, 1f) <= 0)) continue;
            // Remove through vanilla network destruction, without death damage, drops or skill rewards.
            // A pending portal resolves this ID to null and cannot recreate it afterward.
            data.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(data);
            count++;
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, Result, count);
    }

    private static void ShowResult(long sender, int count)
    {
        if (!Plugin.Instance || !ZNet.instance || !Player.m_localPlayer) return;
        long server = ZNet.instance.IsServer() ? ZDOMan.GetSessionID() : ZNet.instance.GetServerPeer()?.m_uid ?? 0;
        if (sender != server) return;
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
            count == 0 ? "No summons to dismiss." : $"Dismissed {count} summon{(count == 1 ? "" : "s")}.");
    }
}
