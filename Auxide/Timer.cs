using System;
using System.Threading;

namespace Auxide
{
    public class Timer
    {
        // The interval between timer ticks in milliseconds
        private float interval;

        // The action to be executed on each timer tick
        private Action action;

        // The number of times the timer should be repeated
        private int repeatCount;

        // The thread that runs the timer
        private Thread timerThread;

        // Indicates whether the timer is running
        private bool isRunning;

        public Timer() { }

        public Timer(float interval, Action action)
        {
            // Validate the interval
            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be a positive integer.");
            }

            // Validate the action
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "The action must not be null.");
            }

            this.interval = interval;
            this.action = action;
        }

        public void Start(float delay = 0)
        {
            // Check if the timer is already running
            if (isRunning)
            {
                return;
            }

            // Set the running flag
            isRunning = true;

            // Create a new thread to run the timer
            timerThread = new Thread(() =>
            {
                // Sleep for the specified delay
                Thread.Sleep((int)delay);

                // Keep track of the number of times the timer has ticked
                int tickCount = 0;

                while (isRunning)
                {
                    // Sleep for the specified interval
                    Thread.Sleep((int)interval);

                    // Execute the action
                    action();

                    // Increment the tick count
                    tickCount++;

                    // Stop the timer if the repeat count is reached
                    if (repeatCount > 0 && tickCount >= repeatCount)
                    {
                        isRunning = false;
                    }

                    // Stop the timer if it is a one-time timer
                    if (interval == Timeout.Infinite)
                    {
                        isRunning = false;
                    }
                }
            });

            // Start the timer thread
            timerThread.Start();
        }

        public void Stop()
        {
            // Check if the timer is already stopped
            if (!isRunning)
            {
                return;
            }

            // Clear the running flag
            isRunning = false;

            // Join the timer thread
            timerThread.Join();
        }

        public Timer Once(float delay, Action action)
        {
            Timer timer = new Timer
            {
                // Set the interval to Timeout.Infinite to stop the timer after it elapses
                interval = Timeout.Infinite,
                // Set the action to be executed on the timer tick
                action = action
            };

            timer.Start(delay);
            return timer;
        }

        public void Destroy()
        {
            this.Destroy();
        }

        public void Repeat(float interval, int repeatCount, Action action)
        {
            // Validate the interval
            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be a positive integer.");
            }

            // Validate the repeat count
            if (repeatCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatCount), "The repeat count must be a positive integer.");
            }

            // Set the interval and repeat count
            this.interval = interval;
            this.repeatCount = repeatCount;

            // Set the action to be executed on the timer tick
            this.action = action ?? throw new ArgumentNullException(nameof(action), "The action must not be null.");

            // Start the timer
            Start();
        }
    }
}
