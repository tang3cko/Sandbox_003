namespace EscapeRoom;

public interface IInteractable
{
    int InteractionPriority { get; }
    bool CanBeInteractedBy(IInteractor interactor);
    InteractionContext BuildContext(IInteractor interactor, string glyph, float holdProgress);
    void OnFocusEnter(IInteractor interactor);
    void OnFocusExit(IInteractor interactor);
    InteractionResult Interact(IInteractor interactor);
}
