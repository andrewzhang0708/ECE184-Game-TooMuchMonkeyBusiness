using System.Collections.Generic;
using UnityEngine;

public class Booboo : MonoBehaviour
{
    [Header("Bamboo Sections")]
    [Tooltip("Optional. Leave empty to use this object's direct children as bamboo sections.")]
    [SerializeField] private Transform[] bambooSections;
    [SerializeField] private float heightTolerance = 0.05f;

    [Header("Falling")]
    [SerializeField, Min(0f)] private float gravityMultiplier = 2.5f;

    private readonly List<Section> sections = new List<Section>();
    private bool initialized;

    private class Section
    {
        public Transform transform;
        public Rigidbody body;
        public bool released;
    }

    private void Awake()
    {
        InitializeSections();
    }

    private void FixedUpdate()
    {
        if (gravityMultiplier <= 1f)
        {
            return;
        }

        for (int i = 0; i < sections.Count; i++)
        {
            Section section = sections[i];
            if (section.released && section.body != null && !section.body.isKinematic)
            {
                section.body.AddForce(
                    Physics.gravity * (gravityMultiplier - 1f),
                    ForceMode.Acceleration
                );
            }
        }
    }

    public void Hit(
        Collider hitCollider,
        Vector3 hitPosition,
        float upVelocity,
        float destroyDelay
    )
    {
        InitializeSections();

        Section hitSection = FindSection(hitCollider);
        if (hitSection == null)
        {
            return;
        }

        float hitHeight = hitSection.transform.position.y;

        for (int i = 0; i < sections.Count; i++)
        {
            Section section = sections[i];
            if (section == hitSection)
            {
                continue;
            }

            if (section.transform == null)
            {
                continue;
            }

            if (section.transform.position.y > hitHeight + heightTolerance)
            {
                ReleaseSection(section);
            }
        }

        DefeatHitSection(
            hitSection,
            hitPosition,
            upVelocity,
            destroyDelay
        );
    }

    private void InitializeSections()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        sections.Clear();

        if (bambooSections == null || bambooSections.Length == 0)
        {
            bambooSections = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                bambooSections[i] = transform.GetChild(i);
            }
        }

        for (int i = 0; i < bambooSections.Length; i++)
        {
            Transform sectionTransform = bambooSections[i];
            if (sectionTransform == null)
            {
                continue;
            }

            Rigidbody body = sectionTransform.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = sectionTransform.gameObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.constraints =
                RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            sections.Add(new Section
            {
                transform = sectionTransform,
                body = body
            });
        }
    }

    private Section FindSection(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        for (int i = 0; i < sections.Count; i++)
        {
            Section section = sections[i];
            if (section.transform == null)
            {
                continue;
            }

            if (hitCollider.transform == section.transform ||
                hitCollider.transform.IsChildOf(section.transform))
            {
                return section;
            }
        }

        return null;
    }

    private void ReleaseSection(Section section)
    {
        if (section == null || section.released || section.body == null)
        {
            return;
        }

        section.released = true;
        section.transform.SetParent(null, true);
        section.body.isKinematic = false;
        section.body.useGravity = true;
        section.body.constraints =
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation;
        section.body.WakeUp();
    }

    private void DefeatHitSection(
        Section section,
        Vector3 hitPosition,
        float upVelocity,
        float destroyDelay
    )
    {
        if (section == null || section.transform == null)
        {
            return;
        }

        GameObject hitSectionObject = section.transform.gameObject;
        section.transform.SetParent(null, true);

        DefeatedEnemyFall.Defeat(
            hitSectionObject,
            hitPosition,
            upVelocity,
            0f,
            gravityMultiplier,
            destroyDelay
        );

        Rigidbody hitBody = hitSectionObject.GetComponent<Rigidbody>();
        if (hitBody != null)
        {
            hitBody.constraints =
                RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotation;
        }
    }
}
