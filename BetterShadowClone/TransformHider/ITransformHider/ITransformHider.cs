namespace NAK.BetterShadowClone;

public interface ITransformHider : IDisposable
{
    bool IsActive { get; set; }
    bool IsValid { get; }
    bool Process();
    bool PostProcess();
    void HideTransform();
    void ShowTransform();
}