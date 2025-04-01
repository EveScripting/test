using System;

namespace SharedComponents.Py.D3DDetour
{
    public abstract class D3DHook
    {
        public delegate void OnFrameDelegate();

        protected static readonly object _frameLock = new object();
        public static event EventHandler<EventArgs> OnFrame;
        public static event EventHandler<EventArgs> OnResize;

        public abstract void Initialize();
        public abstract void Remove();

        protected void RaiseEvent(EventArgs args)
        {
            lock (_frameLock)
            {
                OnFrame?.Invoke(null, args);
            }
        }

        protected void RaiseResizeEvent(EventArgs args)
        {
            OnResize?.Invoke(null, args);
        }
    }
}