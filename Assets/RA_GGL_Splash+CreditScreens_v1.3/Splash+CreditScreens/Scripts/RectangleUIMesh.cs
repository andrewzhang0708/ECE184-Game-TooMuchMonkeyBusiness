using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RedAthena.SplashScreen
{
    public class RectangleUIMesh : Graphic
    {
        [Header("Start at the top left corner, clockwise order.")]
        [SerializeField] private List<Color> colors;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Vector3 rectSize = GetComponent<RectTransform>().sizeDelta;

            vertexHelper.AddVert(new Vector3(-rectSize.x/2f,rectSize.y/2f,0f),colors[0],Vector4.zero);
            vertexHelper.AddVert(new Vector3(rectSize.x/2f,rectSize.y/2f,0f),colors[1],Vector4.zero);
            vertexHelper.AddVert(new Vector3(rectSize.x/2f,-rectSize.y/2f,0f),colors[2],Vector4.zero);
            vertexHelper.AddVert(new Vector3(-rectSize.x/2f,-rectSize.y/2f, 0f), colors[3], Vector4.zero);
            vertexHelper.AddTriangle(0,1,2);
            vertexHelper.AddTriangle(0,2,3);
        }
    }
}
