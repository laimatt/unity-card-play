using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour {
    [Header("Prefab & Container")]
    [SerializeField] private GameObject cardPrefab; // assign the prefab in Inspector
    [SerializeField] private CardContainer cardContainer; // assign your container

    [Header("Card Data")]
    [Tooltip("CSV file in Resources folder (without .csv extension)")]
    [SerializeField] private string cardDataFile = "CardData";
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

    private List<(string root, string vowelharmony, string firstlast)> cardDatabase = new();

    private void LoadCardDatabase() {
        cardDatabase.Clear();
        TextAsset csvFile = Resources.Load<TextAsset>(cardDataFile);
        
        if (csvFile == null) {
            Debug.LogError($"Failed to load card data file: {cardDataFile}.csv");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        // Skip header line
        for (int i = 1; i < lines.Length; i++) {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length >= 4) {
                cardDatabase.Add((
                    values[1].Trim(), // root
                    values[2].Trim(), // vowelharmony
                    values[3].Trim()  // firstlast
                ));
            }
        }
        
        Debug.Log($"Loaded {cardDatabase.Count} cards from CSV file");
    }

    // Spawn a single card from database entry and sprite
    public void SpawnCard((string root, string vowelharmony, string firstlast) cardData, int power = 0, int element = 0) {
        if (cardPrefab == null || cardContainer == null) {
            Debug.LogWarning("DeckManager: missing prefab or container.");
            return;
        }

        var go = Instantiate(cardPrefab, cardContainer.transform, false); // parent to container
        var view = go.GetComponent<CardView>();
        if (view != null) {
            Sprite powerSprite = (power >= 0 && power < powerIcons.Count) ? powerIcons[power] : null;
            Sprite elementSprite = (element >= 0 && element < elementIcons.Count) ? elementIcons[element] : null;
            view.Initialize(cardData.root, cardData.vowelharmony, cardData.firstlast, power + 1, element, powerSprite, elementSprite);
        }
    }

    // Deal a random hand of unique cards (no replacement). If count > pool size, sampling will wrap.
    public void DealRandomHand(int count) {
        if (imagePool == null || imagePool.Count == 0 || cardDatabase.Count == 0) {
            Debug.LogWarning("DeckManager: imagePool or card database is empty.");
            return;
        }

        // Create a list of indices and shuffle it for sampling without replacement
        var indices = new List<int>(cardDatabase.Count);
        var element = new List<int>(cardDatabase.Count);
        for (int i = 0; i < cardDatabase.Count; i++) {
            indices.Add(i);
            element.Add(i % 3);
        }

        // Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
            int tmp2 = element[i]; element[i] = element[j]; element[j] = tmp2;
        }

        // If count <= database size, take first count; otherwise take with wrap (allow repeats)
        int maxCards = Mathf.Min(indices.Count, imagePool.Count);
        if (count <= maxCards) {
            for (int i = 0; i < count; i++) {
                SpawnCard(cardDatabase[indices[i]], i, element[i]);
            }
        } else {
            // take all shuffled first
            for (int i = 0; i < maxCards; i++) {
                SpawnCard(cardDatabase[indices[i]], i, element[i]);
            }
            // then spawn remaining with random picks (with replacement)
            for (int i = maxCards; i < count; i++) {
                int randomIndex = Random.Range(0, cardDatabase.Count);
                SpawnCard(cardDatabase[randomIndex], i, element[i % 3]);
            }
        }
    }

    private void Start() {
        LoadCardDatabase();
        if (dealOnStart) {
            DealRandomHand(Mathf.Max(0, handSizeOnStart));
        }
    }
}