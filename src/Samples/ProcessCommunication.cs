﻿#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp.Samples {
  public class ProcessCommunication {
    /*
     * Process communication example
     * 
     * Covers:
     *  - Resources: Store
     * 
     * Scenario:
     *  This example shows how to interconnect simulation model elements
     *  together using :class:`~simpy.resources.store.Store` for one-to-one,
     *  asynchronous processes.
     */
    private IEnumerable<Event> MessageGenerator(string name, Simulation env, Store outPipe) {
      // A process which randomly generates messages.
      while (true) {
        // wait for next transmission
        yield return env.Timeout(new Uniform(6, 11));

        // messages are time stamped to later check if the consumer was
        // late getting them.  Note, using event.triggered to do this may
        // result in failure due to FIFO nature of simulation yields.
        // (i.e. if at the same env.now, message_generator puts a message
        // in the pipe first and then message_consumer gets from pipe,
        // the event.triggered will be True in the other order it will be
        // False
        var msg = new object[] { env.Now, string.Format("{0} says hello at {1}", name, env.Now) };
        outPipe.Put(msg);
      }
    }


    private IEnumerable<Event> MessageConsumer(string name, Simulation env, Store inPipe) {
      // A process which consumes messages.
      while (true) {
        // Get event for message pipe
        var get = inPipe.Get();
        yield return get;
        var msg = (object[])get.Value;
        if (((DateTime)msg[0]) < env.Now) {
          // if message was already put into pipe, then
          // message_consumer was late getting to it. Depending on what
          // is being modeled this, may, or may not have some
          // significance
          env.Log("LATE Getting Message: at time {0}: {1} received message: {2}", env.Now, name, msg[1]);
        } else {
          // message_consumer is synchronized with message_generator
          env.Log("at time {0}: {1} received message: {2}.", env.Now, name, msg[1]);
        }

        // Process does some other work, which may result in missing messages
        yield return env.Timeout(new Uniform(4, 9));
      }
    }

    public void Simulate(int rseed = 42) {
      // Setup and start the simulation
      var env = new Simulation(rseed, TimeSpan.FromSeconds(1));
      env.Log("== Process communication ==");

      var pipe = new Store(env);
      env.Process(MessageGenerator("Generator A", env, pipe));
      env.Process(MessageConsumer("Consumer A", env, pipe));

      env.Run(TimeSpan.FromSeconds(100));
    }
  }
}
