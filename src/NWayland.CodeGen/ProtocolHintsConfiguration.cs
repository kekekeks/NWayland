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
                EnumTypeNameHints =
                {
                    // https://github.com/swaywm/wlr-protocols/pull/70
                    { "wlr_layer_shell_unstable_v1", "zwlr_layer_surface_v1", "set_layer", "layer", "zwlr_layer_shell_v1.layer" }
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
