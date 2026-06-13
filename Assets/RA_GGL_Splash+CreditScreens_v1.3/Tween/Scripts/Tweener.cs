using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RedAthena.Tween
{
    public class Tweener : MonoBehaviour
    {
        private Dictionary<int, TweenerTask> tweenerTasks;
        [SerializeField] private int tweenerTasksPoolSize = 20;
        private Queue<TweenerTask> tweenerTasksPool;

        private int tweenerTaskId;


        private void Awake()
        {
            tweenerTasks = new Dictionary<int, TweenerTask>();
            tweenerTasksPool = new Queue<TweenerTask>();
            for(int i=0; i<tweenerTasksPoolSize; i++)
            {
                tweenerTasksPool.Enqueue(new TweenerTask());
            }
            tweenerTaskId = 0;
        }
        private void Update()
        {
            List<KeyValuePair<int, TweenerTask>> tweenerTasksCopy = tweenerTasks.ToList();
            foreach(KeyValuePair<int, TweenerTask> keyValuePair in tweenerTasksCopy)
            {
                try
                {
                    keyValuePair.Value.ElapseTask(Time.deltaTime);
                }
                catch(Exception exception)
                {
                    Debug.LogWarning($"Tweener {keyValuePair.Key} exception: {exception}.");
                }
            }
            foreach(KeyValuePair<int, TweenerTask> item in tweenerTasksCopy.Where(kvp => kvp.Value.isFinished))
            {
                tweenerTasksPool.Enqueue(item.Value);
                tweenerTasks.Remove(item.Key);
            }
        }


        public int CreateTweenerTaskSpeed(Action<object> action, Func<float, object> interpolation, float interpolationSpeed, bool isReversedInterpolation=false, TweenerTaskRepeat tweenerTaskRepeat=TweenerTaskRepeat.NONE, Action<int> endCallback=null)
        {
            if(0 == tweenerTasksPool.Count)
            {
                tweenerTasksPool.Enqueue(new TweenerTask());
            }

            tweenerTasks[tweenerTaskId] = tweenerTasksPool.Dequeue();
            tweenerTasks[tweenerTaskId].Initialize(action, interpolation, interpolationSpeed, isReversedInterpolation, tweenerTaskRepeat, endCallback);
            tweenerTasks[tweenerTaskId].id = tweenerTaskId;

            int returnValue = tweenerTaskId;
            tweenerTaskId = (tweenerTaskId+1)%2000000000;
            return returnValue;
        }
        public int CreateTweenerTaskPeriod(Action<object> action, Func<float, object> interpolation, float interpolationPeriod, bool isReversedInterpolation=false, TweenerTaskRepeat tweenerTaskRepeat=TweenerTaskRepeat.NONE, Action<int> endCallback=null)
        {
            return CreateTweenerTaskSpeed(action, interpolation, 1f/interpolationPeriod, isReversedInterpolation, tweenerTaskRepeat, endCallback);
        }

        public float GetTweenerTaskInterpolationAmount(int id)
        {
            if(!tweenerTasks.ContainsKey(id))
            {
                return -1f;
            }
            return tweenerTasks[id].interpolationAmount;
        }
        public void SetTweenerTaskInterpolationAmount(int id, float interpolationAmount)
        {
            if(!tweenerTasks.ContainsKey(id))
            {
                return;
            }
            try
            {
                tweenerTasks[id].DoTask(interpolationAmount);
            }
            catch(Exception exception)
            {
                Debug.Log($"Tweener {tweenerTaskId} exception: {exception}.");
                tweenerTasksPool.Enqueue(tweenerTasks[id]);
                tweenerTasks.Remove(id);
            }
        }

        public void PauseTweenerTask(int id)
        {
            if(!tweenerTasks.ContainsKey(id))
            {
                return;
            }

            tweenerTasks[id].PauseTask();
        }
        public void ResumeTweenerTask(int id)
        {
            if(!tweenerTasks.ContainsKey(id))
            {
                return;
            }

            tweenerTasks[id].ResumeTask();
        }

        public void RemoveTweenerTask(int id)
        {
            if(!tweenerTasks.ContainsKey(id))
            {
                return;
            }

            tweenerTasksPool.Enqueue(tweenerTasks[id]);
            tweenerTasks.Remove(id);
        }

        public void RemoveAllTweenerTasks()
        {
            foreach(KeyValuePair<int, TweenerTask> keyValuePair in tweenerTasks)
            {
                tweenerTasksPool.Enqueue(keyValuePair.Value);
            }
            tweenerTasks.Clear();
        }
    }
}
