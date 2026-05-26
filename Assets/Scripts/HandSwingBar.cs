using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class HandSwingBar : MonoBehaviour
{
    private static readonly List<HandSwingBar> ActiveBars = new List<HandSwingBar>();

    [Tooltip("Optional exact world point the player hand should grab. Uses this transform if assigned, otherwise this object's position.")]
    [SerializeField] private Transform grabPoint;

    [Header("Outline")]
    [SerializeField] private bool useOutline = true;
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 5f;

    private Outline outline;

    public Vector3 GrabPoint => grabPoint != null ? grabPoint.position : transform.position;
    public static IReadOnlyList<HandSwingBar> Bars => ActiveBars;

    private void OnEnable()
    {
        if (!ActiveBars.Contains(this))
        {
            ActiveBars.Add(this);
        }
    }

    private void OnDisable()
    {
        SetHighlighted(false);
        ActiveBars.Remove(this);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (!useOutline)
        {
            return;
        }

        EnsureOutline();

        if (outline == null)
        {
            return;
        }

        outline.enabled = highlighted;
    }

    private void EnsureOutline()
    {
        if (outline != null)
        {
            return;
        }

        outline = GetComponent<Outline>();

        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled = false;
    }

    private void Reset()
    {
        Collider barCollider = GetComponent<Collider>();
        barCollider.isTrigger = true;
    }
}
