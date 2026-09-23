using UnityEngine;

/// <summary>
/// Holds the destination URL for a social/contact link button on the AR business card,
/// and opens it (with visual + audio press feedback) when the button is tapped.
///
/// Visual feedback comes from the Button component's own Color Tint transition
/// (Normal/Highlighted/Pressed colors), already configured in the scene. Audio
/// feedback plays a short click sound through an AudioSource on this GameObject.
/// Accessibility: buttons use large (130x130) tap targets and a high-contrast
/// icon-on-solid-circle design so they remain easy to see and hit on a phone screen.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SocialLinkButton : MonoBehaviour
{
    [Tooltip("URL opened when this button is pressed (e.g. https://github.com/you, or mailto:you@example.com)")]
    public string url;

    [Tooltip("Short sound played through this GameObject's AudioSource when the button is pressed.")]
    public AudioClip clickSound;

    /// <summary>Wired to this button's OnClick() event. Plays feedback and opens the link.</summary>
    public void OpenLink()
    {
        if (clickSound != null)
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning($"SocialLinkButton on '{name}' has no URL set.");
        }
    }
}
