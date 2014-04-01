using System;

namespace SimSharp.Samples {
  class RunAllSamples {
    public static void Main(string[] args) {
      new BankRenege().Simulate();
      Console.WriteLine();
      new GasStationRefueling().Simulate();
      Console.WriteLine();
      new MachineShop().Simulate();
      Console.WriteLine();
      new ProcessCommunication().Simulate();
      Console.WriteLine();
      new SteelFactory().Simulate();
    }
  }
}
