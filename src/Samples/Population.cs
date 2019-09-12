#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using Samples.Probablities.Age;

namespace SimSharp.Samples
{
    public class Population : ISimulate
    {
        private const int RandomSeed = 4;

        // average on ebirth every 30 minutes.
        private static readonly TimeSpan BirthArrivalTime = TimeSpan.FromMinutes(300);
        // Sigma of birth.
        private static readonly TimeSpan BirthSigma = TimeSpan.FromMinutes(50);

        private class Person : ActiveObject<Simulation>
        {
            public bool Gender {get;}
            public Process Process {get;set;}
            public Person(Simulation env) : base(env)
            {
                // Start
                // Start "working" and "break_machine" processes for this machine.
                // Process = env.Process(Working(repairman));
                // env.Process(BreakMachine());
            }
        }

        int _babies;
        Random random = new Random(RandomSeed);

        private string MaakBsn(Simulation env)
        {
            int rest; string bsn;
            do
            {
                bsn = ""; int total = 0; for (int i = 0; i < 8; i++)
                {
                    int rndDigit = random.Next(0, i == 0 ? 2 : 9);
                    total += rndDigit * (9 - i);
                    bsn += rndDigit;
                }
                rest = total % 11;
            }
            while (rest > 9);
            return bsn + rest;
        }

        public class naam
        {
            public string voornaam { get; set; }
            public bool geslacht { get; set; }

            public string achternaam { get; set; }
        }

        public class Adres
        {
            public string straatnaam { get; set; }
            public int huisnummer { get; set; }
            public string huisletter { get; set; }

            public string toevoeging { get; set; }

            public string woonplaats { get; set; }

            public string postcode { get; set; }
        }


        public static Adres GetRandomAddressFromCity(string city, int seed)
        {
            var csv = new CsvReader(new StreamReader("inspireadressen.csv"));
            var result = csv.GetRecords<Adres>();
            var random = new Random(seed);
            return (from p in result orderby random.Next(int.MaxValue) select p).FirstOrDefault();
        }

        public naam GetRandomFirstName(int seed)
        {
            var r = VoornamenDs[new Random(seed).Next(9700)];
            var r1 = new naam();
            r1.voornaam = r.voornaam.Split(' ')[0];
            r1.geslacht = r.voornaam.Split(' ')[1] == "(V)";
                
            return r1;
        }

        public class Persoon {
            public string Bsn { get; set; }
      public string Voornaam { get; set; }
        public string Achternaam { get; set; }

      public DateTime GeboorteDatum { get; set; }
      public DateTime OverlijdensDatum { get; set; }
      public double Leeftijd { get; set; }
      public string Geslacht { get; set; }
        }

    public void Play() {
      var csv = new CsvReader(new StreamReader("./birth.csv"));
      csv.Configuration.HeaderValidated = null;
      csv.Configuration.MissingFieldFound = null;
      csv.Configuration.HasHeaderRecord = true;
      var personen = csv.GetRecords<Persoon>();
      var overlijden = personen.OrderBy(p => p.OverlijdensDatum);

      foreach (var persoon in personen) {
        DateTime start = persoon.GeboorteDatum;
        Console.WriteLine(persoon.GeboorteDatum);
      }
    }

        public naam GetRandomLastName(int seed)
        {
            return AchternamenDs[new Random(seed).Next(100)];
        }
    
        private naam[] VoornamenDs;
        private naam[] AchternamenDs;

    [Obsolete]
        private IEnumerable<Event> Birth(Simulation env, Resource baby)
        {
          var ageProbability = new AgeProbaility(42);

          var voornamen = new CsvReader(new StreamReader("voornamen.csv"));
          voornamen.Configuration.HeaderValidated = null;
          voornamen.Configuration.MissingFieldFound = null;

          var achternamen = new CsvReader(new StreamReader("achternamen.csv"));
          achternamen.Configuration.HeaderValidated = null;
          achternamen.Configuration.MissingFieldFound = null;
          AchternamenDs = achternamen.GetRecords<naam>().ToArray();
          VoornamenDs = voornamen.GetRecords<naam>().ToArray();

          using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"./birth.csv"))
            {
                file.WriteLine("Bsn,GeboorteDatum,OverlijdensDatum,Leeftijd,Geslacht,Voornaam,Achternaam");
                using (var req = baby.Request()) {
                    while (true){
                        yield return req;
                        var time = env.RandNormal(BirthArrivalTime, BirthSigma);
                        var bsn = MaakBsn(env);
                        var voornaam = GetRandomFirstName(int.Parse(bsn));
                        var achternaam = GetRandomLastName(int.Parse(bsn));
                        var age = ageProbability.Sample();
                        _babies++;
            file.WriteLine(bsn + ","
              + env.Now.ToString() + ","
              + env.Now.AddDays(age * 365)
              + ","
              + age
              + ","
              + voornaam.geslacht
              + "," + voornaam.voornaam
              + "," + achternaam.achternaam); ;

                        file.Flush(); yield return env.Timeout(time);
                    }   
                }
            }
        }

    public Population() {

    }
        [Obsolete]
        public void Simulate()
        {


      // Setup and start the simulation
      var env = new Simulation(DateTime.Now.AddDays(-356*100),randomSeed: RandomSeed);
            env.Log("== Population ==");
            var utilization = new TimeSeriesMonitor(env, name: "Utilization");
            var wip = new TimeSeriesMonitor(env, name: "WIP", collect: true);
            var leadtime = new SampleMonitor(name: "Lead time", collect: true);
            var waitingtime = new SampleMonitor(name: "Waiting time", collect: true);
            utilization.Reset();
            var baby = new Resource(env, capacity: 1) {
            };
            env.Process(Birth(env,baby));
            // run simulation for 80 years
            env.Run(TimeSpan.FromDays(365*100));
            env.Log("");
            env.Log("Detailed results from the last run:");
            env.Log("babies born." + _babies);
        }
    }
}