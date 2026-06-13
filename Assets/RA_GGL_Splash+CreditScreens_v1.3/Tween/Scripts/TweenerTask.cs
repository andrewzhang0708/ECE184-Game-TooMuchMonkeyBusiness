using System;
using UnityEngine;

namespace RedAthena.Tween
{
    public class TweenerTask
    {
        public int id;

        public Action<object> action;
        public Func<float, object> interpolation;
        public float interpolationSpeed; // interpolationAmount is increased by interpolationSpeed per second

        public float isReversedTweening;
        public TweenerTaskRepeat tweenerTaskRepeat;
        public float isTweeningBack;

        public bool isPaused;
        public bool isFinished;
        public float interpolationAmount;

        public Action<int> endCallback;

        public TweenerTask() : this(null, null, 0f,false,TweenerTaskRepeat.NONE, null)
        {
        }
        public TweenerTask
        (
            Action<object> action,
            Func<float, object> interpolation,
            float interpolationSpeed,
            bool isReversedTweening,
            TweenerTaskRepeat tweenerTaskRepeat,
            Action<int> endCallback
        )
        {
            Initialize(action, interpolation, interpolationSpeed, isReversedTweening, tweenerTaskRepeat, endCallback);
        }
        public void Initialize
        (
            Action<object> action,
            Func<float, object> interpolation,
            float interpolationSpeed,
            bool isReversedTweening,
            TweenerTaskRepeat tweenerTaskRepeat,
            Action<int> endCallback
        )
        {
            this.action = action;
            this.interpolation = interpolation;
            this.interpolationSpeed = interpolationSpeed;

            if(isReversedTweening)
            {
                this.isReversedTweening = -1f;
                this.interpolationAmount = 1f;
            }
            else
            {
                this.isReversedTweening = 1f;
                this.interpolationAmount = 0f;
            }

            this.tweenerTaskRepeat = tweenerTaskRepeat;
            this.isTweeningBack = 1f;

            this.isPaused = false;
            this.isFinished = false;

            this.endCallback = endCallback;
        }

        public void ElapseTask(float deltaTime)
        {
            try
            {
                DoTask(interpolationAmount + isReversedTweening*isTweeningBack * interpolationSpeed*deltaTime);
            }
            catch(Exception exception)
            {
                throw exception;
            }
        }
        public void DoTask(float interpolationAmount)
        {
            if(isPaused || isFinished)
            {
                return;
            }

            this.interpolationAmount = interpolationAmount;
            this.interpolationAmount = Mathf.Clamp(this.interpolationAmount,0f,1f);
            CheckFinished();
            CheckRepeat();

            try
            {
                action.Invoke(interpolation(this.interpolationAmount));
            }
            catch(Exception exception)
            {
                isFinished = true;
                throw exception;
            }
        }

        public void PauseTask()
        {
            isPaused = true;
        }
        public void ResumeTask()
        {
            isPaused = false;
        }

        private void CheckFinished()
        {
            if(TweenerTaskRepeat.NONE != tweenerTaskRepeat)
            {
                return;
            }

            if(isReversedTweening<0f && Mathf.Approximately(interpolationAmount, 0f))
            {
                isFinished = true;
            }
            else if(isReversedTweening>0f && Mathf.Approximately(interpolationAmount, 1f))
            {
                isFinished = true;
            }

            if(isFinished && null != endCallback)
            {
                try
                {
                    endCallback.Invoke(this.id);
                }
                catch(Exception exception)
                {
                    throw exception;
                }
            }
        }

        private void CheckRepeat()
        {
            if(TweenerTaskRepeat.NONE == tweenerTaskRepeat)
            {
                return;
            }

            if(TweenerTaskRepeat.BOUNCE == tweenerTaskRepeat)
            {
                if(isTweeningBack*isReversedTweening<0f && Mathf.Approximately(interpolationAmount, 0f))
                {
                    isTweeningBack *= -1f;
                }
                else if(isTweeningBack*isReversedTweening>0f && Mathf.Approximately(interpolationAmount, 1f))
                {
                    isTweeningBack *= -1f;
                }
            }
            else if(TweenerTaskRepeat.RESTART == tweenerTaskRepeat)
            {
                if(isTweeningBack*isReversedTweening<0f && Mathf.Approximately(interpolationAmount, 0f))
                {
                    interpolationAmount = 1f;
                }
                else if(isTweeningBack*isReversedTweening>0f && Mathf.Approximately(interpolationAmount, 1f))
                {
                    interpolationAmount = 0f;
                }
            }
        }
    }
}
