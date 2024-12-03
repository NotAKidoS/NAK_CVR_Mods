namespace NAK.ShareBubbles.Impl;

public interface IShareBubbleImpl
{
    bool IsPermitted { get; } // Is user permitted to use the content (Public, Owned, Shared)
    string AuthorId { get; } // Author ID of the content
    void Initialize(ShareBubble shareBubble);
    Task FetchContentInfo(); // Load the content info from the API
    void ViewDetailsPage(); // Open the details page for the content
    void EquipContent(); // Equip the content (Switch/Select)
    void HandleClaimAccept(string userId, Action<bool> onClaimActionCompleted); // Handle the claim action (Share via API)
    void Cleanup(); // Cleanup any resources
}