﻿using System;
using System.Collections.Generic;

namespace Auxide
{
    public class ActionQueue
    {
        // The queue of actions to be executed
        public Queue<Action> actions;

        public ActionQueue()
        {
            // Initialize the queue of actions
            actions = new Queue<Action>();
        }

        public void Enqueue(Action action)
        {
            // Add the action to the queue
            actions.Enqueue(action);
        }

        public Action Dequeue()
        {
            // Remove the next action from the queue
            return actions.Dequeue();
        }

        public void Consume(float delta = 0)
        {
            // Execute all queued actions in order after delay
            if (delta > 0)
            {
                Timer timer = new Timer(delta, () => Consume(0));
            }
            while (actions.Count > 0)
            {
                // Dequeue the next action
                Action action = Dequeue();

                // Execute the action
                action();
            }
            //Thread thread = new Thread(() =>
            //{
            //    if (actions.Count > 0)
            //    {
            //        Action action = Dequeue();
            //        action();
            //    }
            //});
            //thread.Start();
        }
    }
}
