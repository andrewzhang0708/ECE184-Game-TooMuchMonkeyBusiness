using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one instance of " + typeof(T).Name + " found.");
            Destroy(this);
        }
        else
        {
            Instance = this as T;
        }
    }
}
