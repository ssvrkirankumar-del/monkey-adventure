using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// URP pipeline and material helper.
    /// Automatic domain-reload triggers have been disabled to prevent editor hangs and reload loops.
    /// Can be invoked on-demand via 'Window > Monkey Adventure > Fix All Materials and URP Pipeline'.
    /// </summary>
    public static class URPSetupPostprocessor
    {
        // Automatic domain reload triggers disabled.
        // URP is configured on-demand via AutoGameBuilder or menu items.
    }
}
