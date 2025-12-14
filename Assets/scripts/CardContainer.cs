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
        return maxHeightDisplacement *
               (1 - Mathf.Pow(index - (cards.Count - 1) / 2f, 2) / Mathf.Pow((cards.Count - 1) / 2f, 2));
    }

    private float GetCardRotation(int index) {
        if (cards.Count < 3) return 0;
        return -maxCardRotation * (index - (cards.Count - 1) / 2f) / ((cards.Count - 1) / 2f);
    }

    void Update() {
        UpdateCards();
    }

    public void PublicUpdate() {
        UpdateCards();
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

    private bool IsCursorInPlayArea() {
        if (cardPlayConfig == null) {
            return false;
        }

        var cursorPosition = Input.mousePosition;

        if (IsCursorInsideRect(cursorPosition, cardPlayConfig.vowelharmonyArea)) {
            var cardView = currentDraggedCard != null ? currentDraggedCard.GetComponent<CardView>() : null;
            if (cardView != null) {
                cardView.representation = cardView.vowelharmony;
            }
            Debug.Log("In vowel harmony area");
            return true;
        }

        if (IsCursorInsideRect(cursorPosition, cardPlayConfig.firstlastArea)) {
            var cardView = currentDraggedCard != null ? currentDraggedCard.GetComponent<CardView>() : null;
            if (cardView != null) {
                cardView.representation = cardView.firstlast;
            }
            Debug.Log("In first/last area");
            return true;
        }

        if (IsCursorInsideRect(cursorPosition, cardPlayConfig.disharmonicArea)) {
            var cardView = currentDraggedCard != null ? currentDraggedCard.GetComponent<CardView>() : null;
            if (cardView != null) {
                cardView.representation = cardView.disharmonic;
            }
            Debug.Log("In disharmonic area");
            return true;
        }

        // if (IsCursorInsideRect(cursorPosition, cardPlayConfig.playArea)) {
        //     return true;
        // }

        return false;
    }

    private static bool IsCursorInsideRect(Vector3 cursorPosition, RectTransform rect) {
        if (rect == null) {
            return false;
        }

        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return cursorPosition.x > corners[0].x &&
               cursorPosition.x < corners[2].x &&
               cursorPosition.y > corners[0].y &&
               cursorPosition.y < corners[2].y;
    }

    private void SetCardsUIStyles() {
    }

    private void SetCardsUILayers() {
        for (var i = 0; i < cards.Count; i++) {
            var layerOrder = (layoutDirection == LayoutDirection.LeftToRight || layoutDirection == LayoutDirection.TopToBottom)
                ? i
                : cards.Count - 1 - i;
            cards[i].uiLayer = zoomConfig.defaultSortOrder + layerOrder;
        }
    }

    private void UpdateCardOrder() {
        if (!allowCardRepositioning || currentDraggedCard == null) return;

        var isVerticalLayout = layoutDirection == LayoutDirection.TopToBottom || layoutDirection == LayoutDirection.BottomToTop;
        int newCardIdx;

        if (isVerticalLayout) {
            // For vertical layouts, count cards above the dragged card
            newCardIdx = cards.Count(card => currentDraggedCard.transform.position.y < card.transform.position.y);
        }
        else {
            // For horizontal layouts, count cards to the left of the dragged card
            newCardIdx = cards.Count(card => currentDraggedCard.transform.position.x > card.transform.position.x);
        }

        var originalCardIdx = cards.IndexOf(currentDraggedCard);
        if (newCardIdx != originalCardIdx) {
            cards.RemoveAt(originalCardIdx);
            if (newCardIdx > originalCardIdx && newCardIdx < cards.Count - 1) {
                newCardIdx--;
            }

            cards.Insert(newCardIdx, currentDraggedCard);
        }
        currentDraggedCard.transform.SetSiblingIndex(newCardIdx);
    }

    private void SetCardsPosition() {
        var isVerticalLayout = layoutDirection == LayoutDirection.TopToBottom || layoutDirection == LayoutDirection.BottomToTop;

        if (isVerticalLayout) {
            var cardsTotalHeight = cards.Sum(card => card.height * card.transform.lossyScale.y);
            if (cards.Count > 1) {
                if (forceFitContainer) {
                    DistributeChildrenToFitContainerVertical(cardsTotalHeight);
                }
                else {
                    DistributeChildrenWithoutOverlapVertical(cardsTotalHeight);
                }
            }
            else {
                DistributeChildrenWithoutOverlapVertical(cardsTotalHeight);
            }
        }
        else {
            var cardsTotalWidth = cards.Sum(card => card.width * card.transform.lossyScale.x);
            if (cards.Count > 1) {
                if (forceFitContainer) {
                    DistributeChildrenToFitContainer(cardsTotalWidth);
                }
                else {
                    DistributeChildrenWithoutOverlap(cardsTotalWidth);
                }
            }
            else {
                DistributeChildrenWithoutOverlap(cardsTotalWidth);
            }
        }
    }

    private void DistributeChildrenToFitContainer(float childrenTotalWidth) {
        var width = rectTransform.rect.width * transform.lossyScale.x;
        var slots = Mathf.Max(1, cards.Count - 1);
        if (useCenterToCenterSpacing) {
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
            distanceBetweenChildren = Mathf.Max(distanceBetweenChildren, minSpacingWhenFitting);
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

    private void DistributeChildrenToFitContainerVertical(float childrenTotalHeight) {
        var height = rectTransform.rect.height * transform.lossyScale.y;
        var slots = Mathf.Max(1, cards.Count - 1);
        if (useCenterToCenterSpacing) {
            var topEdge = transform.position.y + height / 2;
            var bottomEdge = transform.position.y - height / 2;
            var firstHalf = cards[0].height * cards[0].transform.lossyScale.y / 2f;
            var lastHalf = cards[cards.Count - 1].height * cards[cards.Count - 1].transform.lossyScale.y / 2f;
            var firstPossibleCenter = topEdge - firstHalf;
            var lastPossibleCenter = bottomEdge + lastHalf;
            var maxAvailableSpan = firstPossibleCenter - lastPossibleCenter;
            var desiredCenterSpacing = maxAvailableSpan / (float)slots;
            var centerSpacing = Mathf.Max(desiredCenterSpacing, minSpacingWhenFitting);
            var totalSpan = centerSpacing * slots;

            float firstCenter;
            switch (alignment) {
                case CardAlignment.Left: // Top alignment for vertical
                    firstCenter = firstPossibleCenter;
                    break;
                case CardAlignment.Center:
                    // Center the entire span of cards vertically
                    var spanMidpoint = totalSpan / 2f;
                    firstCenter = transform.position.y + spanMidpoint;
                    break;
                case CardAlignment.Right: // Bottom alignment for vertical
                    firstCenter = lastPossibleCenter + totalSpan;
                    break;
                default:
                    firstCenter = firstPossibleCenter;
                    break;
            }

            var currentCenter = firstCenter;
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                child.targetPosition = new Vector2(transform.position.x, currentCenter);
                currentCenter -= centerSpacing;
            }
        }
        else {
            var distanceBetweenChildren = (height - childrenTotalHeight) / (float)slots;
            distanceBetweenChildren = Mathf.Max(distanceBetweenChildren, minSpacingWhenFitting);

            float currentY;
            switch (alignment) {
                case CardAlignment.Left: // Top alignment for vertical
                    currentY = transform.position.y + height / 2;
                    break;
                case CardAlignment.Center:
                    // Center the cards vertically
                    var totalUsedHeight = childrenTotalHeight + (distanceBetweenChildren * slots);
                    currentY = transform.position.y + totalUsedHeight / 2;
                    break;
                case CardAlignment.Right: // Bottom alignment for vertical
                    var totalUsedHeightBottom = childrenTotalHeight + (distanceBetweenChildren * slots);
                    currentY = transform.position.y - height / 2 + totalUsedHeightBottom;
                    break;
                default:
                    currentY = transform.position.y + height / 2;
                    break;
            }

            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                var adjustedChildHeight = child.height * child.transform.lossyScale.y;
                child.targetPosition = new Vector2(transform.position.x, currentY - adjustedChildHeight / 2);
                currentY -= adjustedChildHeight + distanceBetweenChildren;
            }
        }
    }

    private void DistributeChildrenWithoutOverlapVertical(float childrenTotalHeight) {
        if (useCenterToCenterSpacing) {
            var totalSpan = interCardSpacing * (cards.Count - 1);
            var containerHeightInGlobalSpace = rectTransform.rect.height * transform.lossyScale.y;
            float firstCenter;
            switch (alignment) {
                case CardAlignment.Left: // Top alignment for vertical
                    var topEdge = transform.position.y + containerHeightInGlobalSpace / 2;
                    firstCenter = topEdge - (cards[0].height * cards[0].transform.lossyScale.y) / 2f;
                    break;
                case CardAlignment.Center:
                    // Center the span of cards vertically
                    var spanMidpoint = totalSpan / 2f;
                    firstCenter = transform.position.y + spanMidpoint;
                    break;
                case CardAlignment.Right: // Bottom alignment for vertical
                    var bottomEdge = transform.position.y - containerHeightInGlobalSpace / 2;
                    var lastHalf = (cards[cards.Count - 1].height * cards[cards.Count - 1].transform.lossyScale.y) / 2f;
                    var lastCenter = bottomEdge + lastHalf;
                    firstCenter = lastCenter + totalSpan;
                    break;
                default:
                    firstCenter = GetAnchorPositionByAlignmentVertical(childrenTotalHeight);
                    break;
            }

            var currentCenter = firstCenter;
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                child.targetPosition = new Vector2(transform.position.x, currentCenter);
                currentCenter -= interCardSpacing;
            }
        }
        else {
            var currentPosition = GetAnchorPositionByAlignmentVertical(childrenTotalHeight);
            var orderedCards = GetOrderedCards();
            foreach (CardWrapper child in orderedCards) {
                var adjustedChildHeight = child.height * child.transform.lossyScale.y;
                child.targetPosition = new Vector2(transform.position.x, currentPosition - adjustedChildHeight / 2);
                currentPosition -= adjustedChildHeight + interCardSpacing;
            }
        }
    }

    private float GetAnchorPositionByAlignmentVertical(float childrenHeight) {
        var containerHeightInGlobalSpace = rectTransform.rect.height * transform.lossyScale.y;
        switch (alignment) {
            case CardAlignment.Left: // Top alignment for vertical
                return transform.position.y + containerHeightInGlobalSpace / 2;
            case CardAlignment.Center:
                return transform.position.y + childrenHeight / 2;
            case CardAlignment.Right: // Bottom alignment for vertical
                return transform.position.y - containerHeightInGlobalSpace / 2 + childrenHeight;
            default:
                return 0;
        }
    }

    private IEnumerable<CardWrapper> GetOrderedCards() {
        if (layoutDirection == LayoutDirection.RightToLeft || layoutDirection == LayoutDirection.BottomToTop) {
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
        if (cardPlayConfig != null && !cardPlayConfig.cardPlayed && IsCursorInPlayArea()) {
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
        if (card == null) {
            return;
        }

        if (cardPlayConfig != null && cardPlayConfig.cardPlayed) {
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

    private void SetUpCards() {
        cards.Clear();
        foreach (Transform card in transform) {
            var wrapper = card.GetComponent<CardWrapper>();
            if (wrapper == null) {
                wrapper = card.gameObject.AddComponent<CardWrapper>();
            }

            cards.Add(wrapper);

            AddOtherComponentsIfNeeded(wrapper);

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

    public CardWrapper SpawnCard() {
        if (cardPrefab == null) {
            Debug.LogError("CardContainer: cardPrefab is null. Assign a card prefab in the inspector.");
            return null;
        }

        var go = Instantiate(cardPrefab, transform);
        go.transform.SetParent(transform, false);

        var wrapper = go.GetComponent<CardWrapper>();
        if (wrapper == null) {
            wrapper = go.AddComponent<CardWrapper>();
        }

        wrapper.zoomConfig = zoomConfig;
        wrapper.animationSpeedConfig = animationSpeedConfig;
        wrapper.eventsConfig = eventsConfig;
        wrapper.preventCardInteraction = preventCardInteraction;
        wrapper.container = this;

        cards.Add(wrapper);
        InitCards();

        return wrapper;
    }

    public List<CardWrapper> SpawnCards(int count) {
        var spawned = new List<CardWrapper>();
        for (int i = 0; i < count; i++) {
            var c = SpawnCard();
            if (c != null) spawned.Add(c);
        }

        return spawned;
    }

    public void SpawnCard(CardWrapper card) {
        if (card == null) {
            return;
        }

        // Ensure the card is parented to this container and shares its configs
        card.transform.SetParent(transform, true);
        card.zoomConfig = zoomConfig;
        card.animationSpeedConfig = animationSpeedConfig;
        card.eventsConfig = eventsConfig;
        card.preventCardInteraction = preventCardInteraction;
        card.container = this;

        if (!cards.Contains(card)) {
            cards.Add(card);
        }

        InitCards();
    }

    public void SetPreventCardInteraction(bool prevent) {
        preventCardInteraction = prevent;
        foreach (var card in cards) {
            if (card != null) {
                card.preventCardInteraction = prevent;
            }
        }
    }

    public void setCardPlayed(bool value) {
        if (cardPlayConfig == null) {
            return;
        }

        cardPlayConfig.cardPlayed = value;
    }

    private void CompleteCardPlay(CardWrapper card) {
        if (card == null) {
            return;
        }

        if (cardPlayConfig == null) {
            Debug.LogError("CardContainer: CardPlayConfig is not assigned.");
            return;
        }

        // Debug.LogWarning("CardContainer: card played");
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
    RightToLeft,
    TopToBottom,
    BottomToTop
}
