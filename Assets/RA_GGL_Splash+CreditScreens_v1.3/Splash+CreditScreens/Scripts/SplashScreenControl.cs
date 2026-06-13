using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RedAthena.Tween;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RedAthena.SplashScreen
{
    public class SplashScreenControl : MonoBehaviour
    {
        [SerializeField] private List<SplashScreenEvent> splashScreenEvents;
        [SerializeField] private Tweener tweener;
        [SerializeField] private Transform canvasTransform;
        [SerializeField] private Image endFadeImage;
        [SerializeField] private Color endFadeColor;
        [SerializeField] private float endFadeTime;
        [SerializeField] private GameObject imagePrefab;

        [SerializeField] private bool allowSkip;
        [SerializeField] private GameObject allowSkipWarning;

        private int splashScreenEventsIndex;
        private Dictionary<int, Image> splashScreenImages;

        private bool isEnding;

        private void OnValidate()
        {
            allowSkipWarning.SetActive(allowSkip);
        }

        private void Awake()
        {
            splashScreenEventsIndex = 0;
            splashScreenImages = new Dictionary<int, Image>();
            isEnding = false;
        }

        private void Start()
        {
            ProcessEvents();
        }

        private void Update()
        {
            if(!Mathf.Approximately(1f, Time.timeScale))
            {
                Debug.LogWarning($"Time scale of {Time.timeScale} does not work with the splash screens. Time scale has been changed to 1.");
                Time.timeScale = 1f;
            }

            if(!allowSkip)
            {
                return;
            }

            #if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.anyKey.wasPressedThisFrame)
            {
                End();
            }
        
            #else
            if(Input.anyKey)
            {
                End();
            }
            #endif
        }

        private void ProcessEvents()
        {
            if(isEnding)
            {
                return;
            }
            while(splashScreenEventsIndex<splashScreenEvents.Count)
            {
                SplashScreenEvent current = splashScreenEvents[splashScreenEventsIndex++];
                switch(current.splashScreenEventType) 
                {
                    case SplashScreenEventType.WAIT:
                        tweener.CreateTweenerTaskPeriod((x)=>x=null, (t)=>0f, current.t, endCallback:(x)=>OnWaitFinished(x));
                        return;
                    case SplashScreenEventType.FADE_IN:
                        tweener.CreateTweenerTaskPeriod((x)=>splashScreenImages[current.id].color = (Color)x, (t)=>Color.Lerp(new Color(1f,1f,1f,0f), Color.white, t), current.t);
                        break;
                    case SplashScreenEventType.FADE_OUT:
                        tweener.CreateTweenerTaskPeriod((x)=>splashScreenImages[current.id].color = (Color)x, (t)=>Color.Lerp(Color.white, new Color(1f,1f,1f,0f), t), current.t);
                        break;
                    case SplashScreenEventType.INSERT:
                        splashScreenImages[current.id] = Instantiate(imagePrefab, canvasTransform).GetComponent<Image>();
                        splashScreenImages[current.id].sprite = current.sprite;
                        RectTransform rectTransform = splashScreenImages[current.id].GetComponent<RectTransform>();
                        rectTransform.anchoredPosition = current.anchoredPosition;
                        rectTransform.sizeDelta = current.sizeDelta;
                        rectTransform.anchorMin = current.anchorMin;
                        rectTransform.anchorMax = current.anchorMax;
                        rectTransform.pivot = current.pivot;
                        break;
                    case SplashScreenEventType.REMOVE:
                        Destroy(splashScreenImages[current.id].gameObject);
                        splashScreenImages.Remove(current.id);
                        break;
                    case SplashScreenEventType.CHANGE:
                        splashScreenImages[current.id].sprite = current.sprite;
                        break;
                    case SplashScreenEventType.BUNDLE:
                        splashScreenEvents.InsertRange(splashScreenEventsIndex, current.bundleEvents);
                        break;
                }
            }
        
            End();
        }

        private void OnWaitFinished(int _)
        {
            ProcessEvents();
        }

        private void End()
        {
            if(isEnding)
            {
                return;
            }
            isEnding = true;

            endFadeImage.transform.SetAsLastSibling();
            Color startColor = new Color(endFadeColor.r,endFadeColor.g, endFadeColor.b, 0f);
            tweener.CreateTweenerTaskPeriod((x)=>endFadeImage.color=(Color)x, (t)=>Color.Lerp(startColor, endFadeColor, t), endFadeTime);
            tweener.CreateTweenerTaskPeriod((x)=>x=null, (t)=>Mathf.Lerp(0f, 1f, t), endFadeTime, endCallback:(x)=>ToNextScene());
        }

        private void ToNextScene()
        {
            SceneManager.LoadScene(1);
        }
    }
}
