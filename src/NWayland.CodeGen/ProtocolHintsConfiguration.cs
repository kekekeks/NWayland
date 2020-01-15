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
                ProtocolBlacklist =
                {
                    // Predates unstable naming scheme and causes conflicts with the stable one
                    "xdg_shell_unstable_v5"
                }
            };
        }
    }
}