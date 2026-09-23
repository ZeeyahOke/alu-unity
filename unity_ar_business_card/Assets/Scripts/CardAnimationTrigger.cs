using UnityEngine;

/// <summary>
/// Restarts this GameObject's default Animation clip from the beginning.
/// Wired up (via code, in BusinessCardSceneBuilder) to the ImageTarget's
/// "On Target Found" event, so the card animates in every time the AR
/// marker is (re)detected.
/// </summary>
[RequireComponent(typeof(Animation))]
public class CardAnimationTrigger : MonoBehaviour
{
    public void PlayEntrance()
    {
        var anim = GetComponent<Animation>();
        if (anim == null) return;
        anim.Stop();
        anim.Rewind();
        anim.Play();
    }
}
