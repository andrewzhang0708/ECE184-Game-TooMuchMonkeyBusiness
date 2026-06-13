using UnityEngine;
using UnityEngine.UI;

namespace RedAthena.Tween
{
    public class TweenerExampleControl : MonoBehaviour
    {
        [SerializeField] private Tweener tweener;

        [SerializeField] private Image colorExample;
        [SerializeField] private RectTransform vector3Example;
        [SerializeField] private Slider floatExample;

        private void Start()
        {
            int colorExampleTweenerId = tweener.CreateTweenerTaskPeriod((x)=>colorExample.color = (Color)x, (t)=>Color.Lerp(Color.white, Color.black, t), 2f, tweenerTaskRepeat:TweenerTaskRepeat.BOUNCE);
            int vector3ExampleTweenerId = tweener.CreateTweenerTaskPeriod((x)=>vector3Example.anchoredPosition3D = (Vector3)x, (t)=>Vector3.Lerp(new Vector3(25f, -150f, 0f), new Vector3(225f, -150f, 0f), t), 2f, tweenerTaskRepeat:TweenerTaskRepeat.BOUNCE);
            int floatExampleTweenerId = tweener.CreateTweenerTaskPeriod((x)=>floatExample.value = (float)x, (t)=>Mathf.Lerp(0f, 1f, t), 2f, tweenerTaskRepeat:TweenerTaskRepeat.NONE, endCallback:(x)=>EndMes(x));
        }

        private void EndMes(int id)
        {
            Debug.Log(id);
        }
    }
}
