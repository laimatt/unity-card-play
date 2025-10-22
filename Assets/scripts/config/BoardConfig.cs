using System;
using events;
using UnityEngine;
using UnityEngine.Events;

namespace config {
    [Serializable]
    public class BoardConfig {
        [SerializeField]
        public RectTransform playArea_1;

        [SerializeField]
        public RectTransform playArea_2;

        [SerializeField]
        public UnityEvent<CardPlayed> OnBothCardsPlayed;
        
        // [SerializeField]
        // public bool destroyOnPlay;

        // [SerializeField]
        // public bool cardPlayed;

    }
}
