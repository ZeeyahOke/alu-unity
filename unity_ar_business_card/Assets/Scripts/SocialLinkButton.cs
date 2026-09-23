using UnityEngine;

/// <summary>
/// Holds the destination URL for a social/contact link button on the AR business card.
/// Click handling and feedback (visual/audio) are wired onto this in a later task.
/// </summary>
public class SocialLinkButton : MonoBehaviour
{
    [Tooltip("URL opened when this button is pressed (e.g. https://github.com/you, or mailto:you@example.com)")]
    public string url;
}
