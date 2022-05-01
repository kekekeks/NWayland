namespace NWayland.CodeGen
{
    public static class ProtocolHintsConfiguration
    {
        public static WaylandGeneratorHints GetGeneratorHints() => new()
            {
                ArrayTypeNameHints =
                {
                    { "wayland", "wl_keyboard", "enter", "keys", "int" },
                    { "xdg_shell", "xdg_toplevel", "configure", "states", "StateEnum" }
                },
                ProtocolBlacklist =
                {
                    // Predates unstable naming scheme and causes conflicts with the stable one
                    "xdg_shell_unstable_v5",
                    "drm_lease_v1",
                    "ext_session_lock_v1"
                }
            };
    }
}
