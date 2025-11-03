using System.Collections.Generic;
using System.Linq;
using config;
using DefaultNamespace;
using events;
using UnityEngine;
using UnityEngine.UI;

public class CardContainer : MonoBehaviour {
    [Header("Constraints")]
    [SerializeField]
    private bool forceFitContainer;

    [SerializeField]
    private bool preventCardInteraction;

    [Header("Alignment")]
    [SerializeField]
    private CardAlignment alignment = CardAlignment.Center;

    [SerializeField]
    private LayoutDirection layoutDirection = LayoutDirection.LeftToRight;

    [SerializeField]
    private bool allowCardRepositioning = true;

    [Header("Rotation")]
    [SerializeField]
    [Range(-90f, 90f)]
    private float maxCardRotation;

    [SerializeField]
    private float maxHeightDisplacement;

    [SerializeField]
    private ZoomConfig zoomConfig;

    [SerializeField]
    private AnimationSpeedConfig animationSpeedConfig;

    [SerializeField]
    private CardPlayConfig cardPlayConfig;
    
    [Header("Events")]
    [SerializeField]
    public EventsConfig eventsConfig;

    [Header("Spawning")]
    [SerializeField]
    private GameObject cardPrefab;
    
    [Header("Anchors")]
    [SerializeField]
    private Vector2 childAnchorMin = new Vector2(0f, 0.5f);

    [SerializeField]
    private Vector2 childAnchorMax = new Vector2(0f, 0.5f);
    
    [Header("Spacing")]
    [Tooltip("Extra horizontal spacing (in world units) added between cards when not forcing fit to container.")]
    [SerializeField]
    private float interCardSpacing = 0f;

    [Tooltip("Minimum spacing (in world units) between cards when forceFitContainer is enabled. The distributor will use at least this much spacing when fitting to the container.")]
    [SerializeField]
    private float minSpacingWhenFitting = 0f;

    [Tooltip("When enabled, spacing values are interpreted as center-to-center distance between cards instead of edge-to-edge gap.")]
    [SerializeField]
    private bool useCenterToCenterSpacing = false;
    
    private List<CardWrapper> cards = new();

    private RectTransform rectTransform;
    private CardWrapper currentDraggedCard;

    public IReadOnlyList<CardWrapper> Cards => cards;

    private void Start() {
        rectTransform = GetComponent<RectTransform>();
        InitCards();
    }

    private void InitCards() {
        SetUpCards();
        SetCardsAnchor();
    }

    private void SetCardsRotation() {
        for (var i = 0; i < cards.Count; i++) {
            cards[i].targetRotation = GetCardRotation(i);
            cards[i].targetVerticalDisplacement = GetCardVerticalDisplacement(i);
        }
    }

    private float GetCardVerticalDisplacement(int index) {
        if (cards.Count < 3) return 0;
        // Associate a vertical displacement based on the index in the cards list
        // so that the center card is at max displacement while the edges are at 0 displacement
        return maxHeightDisplacement *
               (1 - Mathf.Pow(index - (cards.Count - 1) / 2f, 2) / Mathf.Pow((cards.Count - 1) / 2f, 2));
    }

    private float GetCardRotation(int index) {
        if (cards.Count < 3) return 0;
        // Associate a rotation based on the index in the cards list
        // so that the first and last cards are at max rotation, mirrored around the center
        return -maxCardRotation * (index - (cards.Count - 1) / 2f) / ((cards.Count - 1) / 2f);
    }

    void Update() {
        UpdateCards();
    }

    public void PublicUpdate() {
        UpdateCards();
    }

    public bool isCardPlayed() {
        return cardPlayConfig.cardPlayed;
    }

    public void setCardPlayed(bool played) {
        cardPlayConfig.cardPlayed = played;
    }

    public void SetPreventCardInteraction(bool value) {
        preventCardInteraction = value;
        foreach (var card in cards) {
            card.preventCardInteraction = value;
        }
    }

    void SetUpCards() {
        cards.Clear();
        foreach (Transform card in transform) {
            var wrapper = card.GetComponent<CardWrapper>();
            if (wrapper == null) {
                wrapper = card.gameObject.AddComponent<CardWrapper>();
            }

            cards.Add(wrapper);

            AddOtherComponentsIfNeeded(wrapper);

            // Pass child card any extra config it should be aware of
            wrapper.zoomConfig = zoomConfig;
            wrapper.animationSpeedConfig = animationSpeedConfig;
            wrapper.eventsConfig = eventsConfig;
            wrapper.preventCardInteraction = preventCardInteraction;
            wrapper.container = this;
        }
    }

    private void AddOtherComponentsIfNeeded(CardWrapper wrapper) {
        var canvas = wrapper.GetComponent<Canvas>();
        if (canvas == null) {
            canvas = wrapper.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;

        if (wrapper.GetComponent<GraphicRaycaster>() == null) {
            wrapper.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void UpdateCards() {
        if (transform.childCount != cards.Count) {
            InitCards();
        }

        if (cards.Count == 0) {
            return;
        }

        SetCardsPosition();
        SetCardsRotation();
        SetCardsUILayers();
        UpdateCardOrder();
    }

    private void SetCardsUILayers() {
        for (var i = 0; i < cards.Count; i++) {
            var layerOrder = (layoutDirection == LayoutDirection.LeftToRight) ? i : cards.Count - 1 - i;
            cards[i].uiLayer = zoomConfig.defaultSortOrder + layerOrder;
        }
    }

    private void UpdateCardOrder() {
        if (!allowCardRepositioning || currentDraggedCard == null) return;

        // Get the index of the dragged card depending on its position
        var newCardIdx = cards.Count(card => currentDraggedCard.transform.position.x > card.transform.position.x);
        var originalCardIdx = cards.IndexOf(currentDraggedCard);
        if (newCardIdx != originalCardIdx) {
            cards.RemoveAt(originalCardIdx);
            if (newCardIdx > originalCardIdx && newCardIdx < cards.Count - 1) {
                newCardIdx--;
            }

            cards.Insert(newCardIdx, currentDraggedCard);
        }
        // Also reorder in the hierarchy
        currentDraggedCard.transform.SetSiblingIndex(newCardIdx);
    }

    private void SetCardsPosition() {
        // Compute the total width of all the cards in global space
        var cardsTotalWidth = cards.Sum(card => card.width * card.transform.lossyScale.x);
        // Compute the width of the container in global space
        var containerWidth = rectTransform.rect.width * transform.lossyScale.x;
        // if (forceFitContainer && cardsTotalWidth > containerWidth) {
        if (cards.Count > 1) {
            DistributeChildrenToFitContainer(cardsTotalWidth);
        }
        else {
            DistributeChildrenWithoutOverlap(cardsTotalWidth);
        }
    }

    private void DistributeChildrenToFitContainer(float childrenTotalWidth) {
        // Get the width of the container
        var width = rectTransform.rect.width * transform.lossyScale.x;
        // Get the distance between each child (handle single-card case)
        var slots = Mathf.Max(1, cards.Count - 1);
        if (useCenterToCenterSpacing) {
            // Compute available span between the first and last possible centers
            var leftEdge = transform.position.x - width / 2;
            var rightEdge = transform.position.x + width / 2;
            var firstHalf = cards[0].width * cards[0].transform.lossyScale.x / 2f;
            var lastHalf = cards[cards.Count - 1].width * cards[cards.Count - 1].transform.lossyScale.x / 2f;
            var firstPossibleCenter = leftEdge + firstHalf;
            var lastPossibleCenter = rightEdge - lastHalf;
            var maxAvailableSpan = lastPossibleCenter - firstPossibleCenter;
            var desiredCenterSpacing = maxAvailableSpan / (float)slots;
            var centerSpacing = Mathf.Max(desiredCenterSpacing, minSpacingWhenFitting);
            var totalSpan = centerSpacing * slots;

            // Choose starting center based on alignment so spacing increase doesn't push layout off-center
            float firstCenter;
            switch (alignment) {
                case CardAlignment.Left:
                    firstCenter = firstPossibleCenter;
                    break;
                case CardAlignment.Center:
                    firstCenter = transform.position.x - totalSpan / 2f;
                    break;
                case CardAlignment.Right:
                    firstCenter = lastPossibleCenter - totalSpan;
                    break;
                default:
                    firstCenter = firstPossibleCenter;
                    break;
            }

            var currentCenter = firstCenter;
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                child.targetPosition = new Vector2(currentCenter, transform.position.y);
                currentCenter += centerSpacing;
            }
        }
        else {
            var distanceBetweenChildren = (width - childrenTotalWidth) / (float)slots;
            // Ensure we respect a minimum spacing if user configured one
            distanceBetweenChildren = Mathf.Max(distanceBetweenChildren, minSpacingWhenFitting);
            // Set all children's positions to be evenly spaced out
            var currentX = transform.position.x - width / 2;
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                var adjustedChildWidth = child.width * child.transform.lossyScale.x;
                child.targetPosition = new Vector2(currentX + adjustedChildWidth / 2, transform.position.y);
                currentX += adjustedChildWidth + distanceBetweenChildren;
            }
        }
    }

    private void DistributeChildrenWithoutOverlap(float childrenTotalWidth) {
        if (useCenterToCenterSpacing) {
            // Compute total center span and starting center depending on alignment
            var totalSpan = interCardSpacing * (cards.Count - 1);
            var containerWidthInGlobalSpace = rectTransform.rect.width * transform.lossyScale.x;
            float firstCenter;
            switch (alignment) {
                case CardAlignment.Left:
                    var leftEdge = transform.position.x - containerWidthInGlobalSpace / 2;
                    firstCenter = leftEdge + (cards[0].width * cards[0].transform.lossyScale.x) / 2f;
                    break;
                case CardAlignment.Center:
                    firstCenter = transform.position.x - totalSpan / 2f;
                    break;
                case CardAlignment.Right:
                    var rightEdge = transform.position.x + containerWidthInGlobalSpace / 2;
                    var lastHalf = (cards[cards.Count - 1].width * cards[cards.Count - 1].transform.lossyScale.x) / 2f;
                    var lastCenter = rightEdge - lastHalf;
                    firstCenter = lastCenter - totalSpan;
                    break;
                default:
                    firstCenter = GetAnchorPositionByAlignment(childrenTotalWidth);
                    break;
            }

            var currentCenter = firstCenter;
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                child.targetPosition = new Vector2(currentCenter, transform.position.y);
                currentCenter += interCardSpacing;
            }
        }
        else {
            var currentPosition = GetAnchorPositionByAlignment(childrenTotalWidth);
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                var adjustedChildWidth = child.width * child.transform.lossyScale.x;
                child.targetPosition = new Vector2(currentPosition + adjustedChildWidth / 2, transform.position.y);
                // Move current position by the child's width plus any configured extra spacing
                currentPosition += adjustedChildWidth + interCardSpacing;
            }
        }
    }

    private float GetAnchorPositionByAlignment(float childrenWidth) {
        var containerWidthInGlobalSpace = rectTransform.rect.width * transform.lossyScale.x;
        switch (alignment) {
            case CardAlignment.Left:
                return transform.position.x - containerWidthInGlobalSpace / 2;
            case CardAlignment.Center:
                return transform.position.x - childrenWidth / 2;
            case CardAlignment.Right:
                return transform.position.x + containerWidthInGlobalSpace / 2 - childrenWidth;
            default:
                return 0;
        }
    }

    private IEnumerable<CardWrapper> GetOrderedCards() {
        if (layoutDirection == LayoutDirection.RightToLeft) {
            return cards.AsEnumerable().Reverse();
        }
        return cards;
    }

    private void SetCardsAnchor() {
        foreach (CardWrapper child in cards) {
            child.SetAnchor(childAnchorMin, childAnchorMax);
        }
    }

    public void OnCardDragStart(CardWrapper card) {
        currentDraggedCard = card;
    }

    public void OnCardDragEnd() {
        // If card is in play area, play it!
        if (!cardPlayConfig.cardPlayed && IsCursorInPlayArea()) {
            CompleteCardPlay(currentDraggedCard);
        }
        currentDraggedCard = null;
    }
    
    public void DestroyCard(CardWrapper card) {
        cards.Remove(card);
        eventsConfig.OnCardDestroy?.Invoke(new CardDestroy(card));
        Destroy(card.gameObject);
    }

    public void PlayCard(CardWrapper card) {
        if (cardPlayConfig.cardPlayed || card == null) {
            return;
        }

        if (!cards.Contains(card)) {
            InitCards();
        }

        if (!cards.Contains(card)) {
            return;
        }

        CompleteCardPlay(card);
    }

    // --- Runtime spawning helpers ---
    // Instantiates a card prefab as a child of this container, initializes CardWrapper and layout.
    public CardWrapper SpawnCard() {
        if (cardPrefab == null) {
            Debug.LogError("CardContainer: cardPrefab is null. Assign a card prefab in the inspector.");
            return null;
        }

        var go = Instantiate(cardPrefab, transform);
        // Ensure RectTransform and required UI components are preserved by using SetParent with worldPositionStays=false
        go.transform.SetParent(transform, false);

        var wrapper = go.GetComponent<CardWrapper>();
        if (wrapper == null) {
            wrapper = go.AddComponent<CardWrapper>();
        }

        // Pass configuration expected by CardContainer
        wrapper.zoomConfig = zoomConfig;
        wrapper.animationSpeedConfig = animationSpeedConfig;
        wrapper.eventsConfig = eventsConfig;
        wrapper.preventCardInteraction = preventCardInteraction;
        wrapper.container = this;

        // Add to internal list and refresh layout
        cards.Add(wrapper);
        InitCards();

        return wrapper;
    }

    // Spawn multiple cards
    public List<CardWrapper> SpawnCards(int count) {
        var spawned = new List<CardWrapper>();
        for (int i = 0; i < count; i++) {
            var c = SpawnCard();
            if (c != null) spawned.Add(c);
        }

        return spawned;
    }

    private bool IsCursorInPlayArea() {
        if (cardPlayConfig.playArea == null) return false;
        
        var cursorPosition = Input.mousePosition;
        var playArea = cardPlayConfig.playArea;
        var playAreaCorners = new Vector3[4];
        playArea.GetWorldCorners(playAreaCorners);
        return cursorPosition.x > playAreaCorners[0].x &&
               cursorPosition.x < playAreaCorners[2].x &&
               cursorPosition.y > playAreaCorners[0].y &&
               cursorPosition.y < playAreaCorners[2].y;
        
    }

    public void SpawnCard(CardWrapper card) {
        Debug.LogWarning("DiscardContainer: card added to discard!");

        // Re-apply this container's configuration to the card
        card.zoomConfig = zoomConfig;
        card.animationSpeedConfig = animationSpeedConfig;
        card.eventsConfig = eventsConfig;
        card.preventCardInteraction = preventCardInteraction;
        card.container = this;

        cards.Add(card);
        // CardContainer detects new children in its Update() and will re-init layout on next frame.
    }

    private void CompleteCardPlay(CardWrapper card) {
        if (card == null) {
            return;
        }

        Debug.LogWarning("CardContainer: card played");
        eventsConfig?.OnCardPlayed?.Invoke(new CardPlayed(card));

        var playArea = cardPlayConfig.playArea;
        Vector3 playCenterWorld = Vector3.zero;
        if (playArea != null) {
            var playAreaCorners = new Vector3[4];
            playArea.GetWorldCorners(playAreaCorners);
            playCenterWorld = new Vector3((playAreaCorners[0].x + playAreaCorners[2].x) / 2f,
                (playAreaCorners[0].y + playAreaCorners[2].y) / 2f,
                playAreaCorners[0].z);
        }

        if (cardPlayConfig.destroyOnPlay) {
            DestroyCard(card);
            return;
        }

        if (cards.Contains(card)) cards.Remove(card);
        card.preventCardInteraction = true;
        card.container = null;

        if (playArea != null) {
            card.transform.SetParent(playArea, true);
            var rect = card.GetComponent<RectTransform>();
            if (rect != null) {
                card.targetPosition = playCenterWorld;
                card.targetRotation = 0;
                card.targetVerticalDisplacement = 0;
                card.uiLayer = 1;
            }
            else {
                card.transform.position = playCenterWorld;
            }
        }

        cardPlayConfig.cardPlayed = true;
    }
}

public enum LayoutDirection {
    LeftToRight,
    RightToLeft
}
