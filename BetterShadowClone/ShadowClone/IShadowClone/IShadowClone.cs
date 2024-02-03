using System;

namespace NAK.BetterShadowClone;

public interface IShadowClone : IDisposable
{
    bool IsValid { get; }
    bool Process();
    void RenderForShadow();
    void RenderForUiCulling();
}