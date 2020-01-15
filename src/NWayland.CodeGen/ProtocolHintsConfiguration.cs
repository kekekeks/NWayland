namespace NWayland.CodeGen
{
    public class ProtocolHintsConfiguration
    {
        public static WaylandGeneratorHints GetGeneratorHints()
        {
            return new WaylandGeneratorHints
            {
                ArrayTypeNameHints =
                {
                    {"wayland", "wl_keyboard", "enter", "keys", "int"}
                },
                EnumTypeNameHints =
                {
                    // https://github.com/swaywm/wlr-protocols/pull/70
                    {"wlr_layer_shell_unstable_v1", "zwlr_layer_surface_v1", "set_layer", "layer", "zwlr_layer_shell_v1.layer"}
                },
                ProtocolBlacklist =
                {
                    // Predates unstable naming scheme and causes conflicts with the stable one
                    "xdg_shell_unstable_v5"
                }
            };
        }
    }
}