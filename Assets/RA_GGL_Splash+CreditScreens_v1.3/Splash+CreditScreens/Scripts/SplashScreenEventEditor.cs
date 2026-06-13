#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace RedAthena.SplashScreen
{
    [CustomEditor(typeof(SplashScreenEvent))]
    [CanEditMultipleObjects]
    public class SplashScreenEventEditor : Editor
    {
        private SerializedProperty splashScreenEventType;
        private SerializedProperty id;
        private SerializedProperty t;
        private SerializedProperty sprite;
        private SerializedProperty anchoredPosition;
        private SerializedProperty sizeDelta;
        private SerializedProperty anchorMin;
        private SerializedProperty anchorMax;
        private SerializedProperty pivot;
        private SerializedProperty bundleEvents;

        private void OnEnable()
        {
            SplashScreenEvent current = (SplashScreenEvent)target;
            splashScreenEventType = serializedObject.FindProperty(nameof(current.splashScreenEventType));
            id = serializedObject.FindProperty(nameof(current.id));
            t = serializedObject.FindProperty(nameof(current.t));
            sprite = serializedObject.FindProperty(nameof(current.sprite));
            anchoredPosition = serializedObject.FindProperty(nameof(current.anchoredPosition));
            sizeDelta = serializedObject.FindProperty(nameof(current.sizeDelta));
            anchorMin = serializedObject.FindProperty(nameof(current.anchorMin));
            anchorMax = serializedObject.FindProperty(nameof(current.anchorMax));
            pivot = serializedObject.FindProperty(nameof(current.pivot));
            bundleEvents = serializedObject.FindProperty(nameof(current.bundleEvents));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(splashScreenEventType, new GUIContent("Event Type"));

            switch((SplashScreenEventType)splashScreenEventType.intValue) 
            {
                case SplashScreenEventType.WAIT:
                    EditorGUILayout.PropertyField(t, new GUIContent("Wait Time"));
                    break;
                case SplashScreenEventType.FADE_IN:
                case SplashScreenEventType.FADE_OUT:
                    EditorGUILayout.PropertyField(id, new GUIContent("ID"));
                    EditorGUILayout.PropertyField(t, new GUIContent("Fade Time"));
                    break;
                case SplashScreenEventType.INSERT:
                    EditorGUILayout.PropertyField(id, new GUIContent("ID"));
                    EditorGUILayout.PropertyField(sprite, new GUIContent("Sprite"));
                    EditorGUILayout.PropertyField(anchoredPosition, new GUIContent("Anchored Position"));
                    EditorGUILayout.PropertyField(sizeDelta, new GUIContent("Size"));
                    EditorGUILayout.PropertyField(anchorMin, new GUIContent("Anchor Min"));
                    EditorGUILayout.PropertyField(anchorMax, new GUIContent("Anchor Max"));
                    EditorGUILayout.PropertyField(pivot, new GUIContent("Pivot"));
                    break;
                case SplashScreenEventType.REMOVE:
                    EditorGUILayout.PropertyField(id, new GUIContent("ID"));
                    break;
                case SplashScreenEventType.CHANGE:
                    EditorGUILayout.PropertyField(id, new GUIContent("ID"));
                    EditorGUILayout.PropertyField(sprite, new GUIContent("Sprite"));
                    break;
                case SplashScreenEventType.BUNDLE:
                    EditorGUILayout.PropertyField(bundleEvents, new GUIContent("Bundle Events"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif