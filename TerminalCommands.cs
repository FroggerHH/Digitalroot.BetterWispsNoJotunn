using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using static BetterWispsNoJotunn.Plugin;
using static Terminal;

namespace BetterWispsNoJotunn;

public static class TerminalCommands
{
    private static bool isServer => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    private static string modName => ModName;


    [HarmonyPatch(typeof(Terminal), nameof(InitTerminal))]
    [HarmonyWrapSafe]
    internal class AddChatCommands
    {
        private static void Postfix()
        {
            new ConsoleCommand("-1", "",
                args =>
                {
                    try
                    {
                        if (!configSync.IsAdmin && !ZNet.instance.IsServer())
                        {
                            args.Context.AddString("You are not an admin on this server.");
                        }
                    }
                    catch (Exception e)
                    {
                        args.Context.AddString("<color=red>Error: " + e.Message + "</color>");
                    }
                }, true);
        }
    }
}