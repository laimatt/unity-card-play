using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour {
    [Header("Prefab & Container")]
    [SerializeField] private GameObject cardPrefab; // assign the prefab in Inspector
    [SerializeField] private CardContainer cardContainer; // assign your container

    [Header("Card Pool")]
    [Tooltip("Pool of artwork sprites to draw random cards from")]
    [SerializeField] private List<Sprite> imagePool = new();

    [Header("Icons")]
    [Tooltip("Sprites used for power icons, indexed by power value")]
    [SerializeField] private List<Sprite> powerIcons = new();
    [Tooltip("Sprites used for element icons, indexed by element value")]
    [SerializeField] private List<Sprite> elementIcons = new();

    [Header("Deal")]
    [SerializeField] private bool dealOnStart = true;
    [SerializeField] private int handSizeOnStart = 9;

    // Spawn a single card from a sprite and optional title
    public void SpawnCard(Sprite artwork, string title = null, int power = 0, int element = 0) {
        if (cardPrefab == null || cardContainer == null) {
            Debug.LogWarning("DeckManager: missing prefab or container.");
            return;
        }

        var go = Instantiate(cardPrefab, cardContainer.transform, false); // parent to container
        var view = go.GetComponent<CardView>();
        if (view != null) {
            Sprite powerSprite = (power >= 0 && power < powerIcons.Count) ? powerIcons[power] : null;
            Sprite elementSprite = (element >= 0 && element < elementIcons.Count) ? elementIcons[element] : null;
            view.Initialize(artwork, title ?? artwork?.name ?? "Card", power + 1, element, powerSprite, elementSprite);
        }

        // CardContainer detects new children in its Update() and will re-init layout on next frame.
    }

    // Deal a random hand of unique cards (no replacement). If count > pool size, sampling will wrap.
    public void DealRandomHand(int count) {
        if (imagePool == null || imagePool.Count == 0) {
            Debug.LogWarning("DeckManager: imagePool is empty. Assign sprites in the inspector.");
            return;
        }

        // Create a list of indices and shuffle it for sampling without replacement
        var indices = new List<int>(imagePool.Count);
        var element = new List<int>(imagePool.Count);
        for (int i = 0; i < imagePool.Count; i++) {
            indices.Add(i);
            element.Add(i % 3);
            // Debug.LogWarning($"DeckManager: adding element {i % 3}.");
        }

        // Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
            int tmp2 = element[i]; element[i] = element[j]; element[j] = tmp2;
        }

        // If count <= pool, take first count; otherwise take with wrap (allow repeats)
        if (count <= indices.Count) {
            for (int i = 0; i < count; i++) {
                var sprite = imagePool[indices[i]];
                SpawnCard(sprite, sprite?.name ?? $"Card_{i}", i, element[i]);
                // Debug.LogWarning($"DeckManager: spawning card {element[i]}.");
            }
        } else {
            // take all shuffled first
            for (int i = 0; i < indices.Count; i++) {
                var sprite = imagePool[indices[i]];
                SpawnCard(sprite, sprite?.name ?? $"Card_{i}", i, element[i]);
            }
            // then spawn remaining with random picks (with replacement)
            for (int i = indices.Count; i < count; i++) {
                var sprite = imagePool[Random.Range(0, imagePool.Count)];
                SpawnCard(sprite, sprite?.name ?? $"Card_{i}", i, element[i]);
                
            }
        }

    }

    private void Start() {
        if (dealOnStart) {
            DealRandomHand(Mathf.Max(0, handSizeOnStart));
        }
        
    }
}