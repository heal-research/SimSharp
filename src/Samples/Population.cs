#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp.Samples
{
    public class Population : ISimulate
    {
        public bool Gender {get;}
        public Process Process {get;set;}
        private class Person : ActiveObject<Simulation>
        {
            public Person(Simulation env) : base(env)
            {
                // Start
                // Start "working" and "break_machine" processes for this machine.
                Process = env.Process(Working(repairman));
                env.Process(BreakMachine());
            }
        }

        private const int RandomSeed = 42; // Life, the Universe and everything

        public void Simulate()
        {
             // Setup and start the simulation
            var env = new Simulation(RandomSeed, TimeSpan.FromSeconds(1));
            var packer = new Resource(env, 1);
            env.Process(Birth(env, packer));
            env.Log("== Population ==");
            env.Run(TimeSpan.FromDays(365*80));
        }

        private IEnumerable<Event> Birth(Simulation env, Resource packer)
        {
            throw new NotImplementedException();
        }
    }
}