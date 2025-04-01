using System;
using SharedComponents.Py.Frameworks;

namespace SharedComponents.Py.D3DDetour
{
    public class StandaloneFramework : IFramework
    {
        private EventHandler<EventArgs> _frameHandler;
        private EventHandler<EventArgs> _resizeHandler;

        public StandaloneFramework()
        {
        }

        public void RegisterFrameHook(EventHandler<EventArgs> frameHandler, EventHandler<EventArgs> resizeHandler = null)
        {
            Pulse.Initialize();
            _frameHandler = frameHandler;
            _resizeHandler = resizeHandler;
            D3DHook.OnFrame += _frameHandler;

            if (_resizeHandler != null)
                D3DHook.OnResize += _resizeHandler;
        }

        public void RegisterLogger(EventHandler<EventArgs> logger)
        {
        }

        #region IDisposable Members

        public void Dispose()
        {
            D3DHook.OnFrame -= _frameHandler;
            if (_resizeHandler != null)
                D3DHook.OnResize -= _resizeHandler;
            Pulse.Shutdown();
        }

        #endregion
    }
}