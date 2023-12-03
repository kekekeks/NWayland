namespace NWayland.Scanner
{
    public static class ProtocolHintsConfiguration
    {
        public static WaylandGeneratorHints GetGeneratorHints() => new()
            {
                ArrayTypeNameHints =
                {
                    { "wayland", "wl_keyboard", "enter", "keys", "int" },
                    { "xdg_shell", "xdg_toplevel", "configure", "states", "StateEnum" },
                    { "xdg_shell", "xdg_toplevel", "wm_capabilities", "capabilities", "WmCapabilitiesEnum" }
                },
                ProtocolBlacklist =
                {
                    // Predates unstable naming scheme and causes conflicts with the stable one
                    "xdg_shell_unstable_v5",
                    // Marked as stable
                    "linux_dmabuf_unstable_v1",
                    // Protocols have more than one method marked as their destructor
                    "drm_lease_v1",
                    "ext_session_lock_v1"
                }
            };
    }
}
