// AnimationQueue.cs — Animation command queue for sequencing visual effects
// Ported from src/game/AnimationQueue.ts

using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Core
{
    public enum AnimationStatus
    {
        PENDING,
        RUNNING,
        COMPLETED,
        CANCELLED,
    }

    public class QueuedAnimation
    {
        public AnimationCommand Command;
        public AnimationStatus Status;
        public float StartTime;
        public float ActualDelay;
        public float ActualDuration;

        public QueuedAnimation(AnimationCommand command)
        {
            Command = command;
            Status = AnimationStatus.PENDING;
            ActualDelay = command.Delay;
            ActualDuration = command.Duration;
            StartTime = 0f;
        }
    }

    public class AnimationQueue
    {
        private readonly List<QueuedAnimation> _queue = new List<QueuedAnimation>();
        private float _currentTime = 0f;
        private float _totalDuration = 0f;

        public void Enqueue(AnimationCommand command)
        {
            float actualDelay = command.Delay;
            float actualDuration = command.Duration;
            var entry = new QueuedAnimation(command)
            {
                StartTime = _totalDuration + actualDelay,
                ActualDelay = actualDelay,
                ActualDuration = actualDuration,
            };
            _queue.Add(entry);
            _totalDuration = entry.StartTime + actualDuration;
        }

        public List<QueuedAnimation> Tick(float deltaTime)
        {
            _currentTime += deltaTime;
            var completed = new List<QueuedAnimation>();

            foreach (var entry in _queue)
            {
                if (entry.Status == AnimationStatus.PENDING && _currentTime >= entry.StartTime)
                {
                    entry.Status = AnimationStatus.RUNNING;
                }
                if (entry.Status == AnimationStatus.RUNNING && _currentTime >= entry.StartTime + entry.ActualDuration)
                {
                    entry.Status = AnimationStatus.COMPLETED;
                    completed.Add(entry);
                }
            }

            return completed;
        }

        public List<QueuedAnimation> GetRunning()
        {
            var result = new List<QueuedAnimation>();
            foreach (var entry in _queue)
            {
                if (entry.Status == AnimationStatus.RUNNING || entry.Status == AnimationStatus.PENDING)
                    result.Add(entry);
            }
            return result;
        }

        public List<AnimationCommand> GetActiveCommands()
        {
            var result = new List<AnimationCommand>();
            foreach (var entry in _queue)
            {
                if (entry.Status == AnimationStatus.RUNNING)
                    result.Add(entry.Command);
            }
            return result;
        }

        public bool IsComplete()
        {
            foreach (var e in _queue)
            {
                if (e.Status != AnimationStatus.COMPLETED)
                    return false;
            }
            return true;
        }

        public void Clear()
        {
            _queue.Clear();
            _currentTime = 0f;
            _totalDuration = 0f;
        }

        public AnimationCommand Peek()
        {
            foreach (var entry in _queue)
            {
                if (entry.Status == AnimationStatus.PENDING || entry.Status == AnimationStatus.RUNNING)
                    return entry.Command;
            }
            return null;
        }

        public float GetProgress()
        {
            if (_queue.Count == 0) return 1f;

            QueuedAnimation active = null;
            foreach (var entry in _queue)
            {
                if (entry.Status == AnimationStatus.RUNNING)
                {
                    active = entry;
                    break;
                }
            }

            if (active == null) return 0f;

            float elapsed = _currentTime - active.StartTime;
            return elapsed / active.ActualDuration;
        }
    }
}
