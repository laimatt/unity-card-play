using System.Collections;
using events;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

// Attach this script to a GameObject with a UI Text (or other display) and wire it to
// the CardContainer -> Events -> OnCardPlayed event in the Inspector.
public class PlayAudio : MonoBehaviour {
    [SerializeField]
    private AudioSource soundEffectAudioSource;



    public void Audio(CardPlayed evt) {
        if (soundEffectAudioSource == null) {
            Debug.LogWarning("PlayAudio: audio is not assigned.");
            return;
        }

        // string path = Path.Combine(Application.streamingAssetsPath);

        var cardName = evt?.card?.gameObject?.name ?? "bido";
        // messageText.clip = cardName;

        AudioClip clip = Resources.Load<AudioClip>($"Sounds/{cardName}");
        // Debug.LogWarning($"PlayAudio: {cardName} ");

        soundEffectAudioSource.clip = clip;

        soundEffectAudioSource.Play();

        // var cardName = evt?.card?.gameObject?.name ?? "Card";
        // messageText.text = $"Played: {cardName}";
    }

}