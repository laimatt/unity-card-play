using System;
using UnityEngine;

namespace config {
    [Serializable]
    public class CardPlayConfig {
        [SerializeField]
        public RectTransform playArea;

        [SerializeField]
        public RectTransform vowelharmonyArea;

        [SerializeField]
        public RectTransform firstlastArea;
        
        [SerializeField]
        public bool destroyOnPlay;

        [SerializeField]
        public bool cardPlayed;

    }
}
