using System;

namespace DGame.FixedPoint
{
    internal class FPTimer : FastListItem
    {
        private FixedPoint64 activeTime;
        private FixedPoint64 currentTime;
        private FixedPoint64 deltaTime;
        private Action<FPTimer> onActive;
        public bool disposed { get; private set; }

        public FPTimer(uint activeTimeInt, FixedPoint64 deltaTime, Action<FPTimer> onActive)
        {
            activeTime = activeTimeInt * 0.001;
            this.deltaTime = deltaTime;
            this.onActive = onActive;
        }

        public void OnUpdate()
        {
            if (disposed)
            {
                return;
            }

            currentTime += deltaTime;

            if (currentTime >= activeTime)
            {
                disposed = true;
                onActive?.Invoke(this);
            }
        }

        public int index { get; set; } = -1;
    }
}
