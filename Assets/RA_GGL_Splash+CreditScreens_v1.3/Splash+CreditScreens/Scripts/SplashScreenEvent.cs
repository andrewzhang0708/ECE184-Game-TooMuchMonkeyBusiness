using UnityEngine;
using System.Collections.Generic;

namespace RedAthena.SplashScreen
{
    public enum SplashScreenEventType
    {
        WAIT,

        FADE_IN,
        FADE_OUT,

        INSERT,
        REMOVE,

        CHANGE,

        BUNDLE,
    }

    [CreateAssetMenu(fileName = "SplashScreenEvent", menuName = "ScriptableObjects/SplashScreen/SplashScreenEvent", order = 0)]
    public class SplashScreenEvent : ScriptableObject
    {
        public SplashScreenEventType splashScreenEventType;

        public int id;
        public float t;

        public Sprite sprite;

        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;

        public List<SplashScreenEvent> bundleEvents;
    }
}
