using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using ExcelDataReader;
using MathNet.Numerics.Distributions;

namespace Samples.Probablities.Age {
  public class AgeProbaility {
    private readonly int seed;
    private Random random;

    private List<Death> probablities;

    public class Death {
      public double Age { get; set; }
      public double Percentage { get; set; }
    }
    
    public AgeProbaility(int seed) {
      this.seed = seed;
      this.random = new Random(seed);
      probablities = new List<Death>();
      System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
      using (var stream = File.Open(@"./Probabilities/Age/statistics.xlsx", FileMode.Open, FileAccess.Read)) {
        using (var reader = ExcelReaderFactory.CreateReader(stream)) {
          var result = reader.AsDataSet();
          for (int i = 1; i < 22; i++) {
            probablities.Add(new Death() { Age = (double)result.Tables[0].Rows[i][0], Percentage = (double)result.Tables[0].Rows[i][3] });
            var s = result.Tables[0].Rows[i][0];

          }
        }
      }
    }

    //var f = new Poisson(18);
    public double Sample() {
      double age=0;
        var s = random.NextDouble() * 0.42;
        for (int i = 1; i < probablities.Count; i++) {
          if (s < probablities[i].Percentage) {
            double minAge = 0;
            double maxAge;
            if (i == 0) {
              minAge = 0;
              maxAge = probablities[i].Age;
            } else {
              minAge = probablities[i - 1].Age;
              maxAge = probablities[i].Age;
            }

          age = (minAge + (maxAge - minAge)) * random.NextDouble();

          }
        }
      return age;
    }
  }
}

