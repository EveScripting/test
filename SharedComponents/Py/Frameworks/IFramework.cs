using System;

namespace SharedComponents.Py.Frameworks
{
    public interface IFramework : IDisposable
    {
        void RegisterFrameHook(EventHandler<EventArgs> frameHandler, EventHandler<EventArgs> resizeEventHandler = null);
        void RegisterLogger(EventHandler<EventArgs> logger);
    }
}