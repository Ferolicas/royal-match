using MCPForUnity.Editor;
using UnityEditor;
using UnityEngine;

namespace RoyalMatch.EditorTools
{
    [InitializeOnLoad]
    internal static class UnityMcpStartup
    {
        static UnityMcpStartup()
        {
            EditorApplication.delayCall += Start;
        }

        private static void Start()
        {
            if (Application.isBatchMode) return;
            McpCiBoot.StartStdioForCi();
        }
    }
}
