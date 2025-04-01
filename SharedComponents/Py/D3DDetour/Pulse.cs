namespace SharedComponents.Py.D3DDetour
{
    public static class Pulse
    {
        public static D3DHook Hook;

        public static void Initialize()
        {
         
            Hook = new D3D11();
            Hook.Initialize();
        }

        public static void Shutdown()
        {
            Hook.Remove();
        }
    }
}