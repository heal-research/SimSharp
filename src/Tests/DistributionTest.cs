#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System.Text;
using Xunit;

namespace SimSharp.Samples {
  public class DistributionData {
    [Fact]
    public void GenerateData() {
      var uniform = new Uniform(0, 1);
      var triangular = new Triangular(0, 4, 3);
      var exponential = new Exponential(1);
      var normal = new Normal(0, 1);
      var logNormal = new LogNormal(1, 0.2);
      var cauchy = new Cauchy(1, 0.5);
      var weibull = new Weibull(1.5, 1);
      var erlang = new Erlang(2, 2);

      var rand = new PcgRandom(13);
      var sb = new StringBuilder();
      sb.AppendLine("Uniform(0,1);Triangular(0,3,4);Exponential(1);Normal(0,1);LogNormal(1,0.2);Cauchy(1,0.5);Weibull(1.5,1);Erlang(2,2)");
      for (var i = 0; i < 1000; i++) {
          sb.Append(uniform.Sample(rand) + ";");
          sb.Append(triangular.Sample(rand) + ";");
          sb.Append(exponential.Sample(rand) + ";");
          sb.Append(normal.Sample(rand) + ";");
          sb.Append(logNormal.Sample(rand) + ";");
          sb.Append(cauchy.Sample(rand) + ";");
          sb.Append(weibull.Sample(rand) + ";");
          sb.AppendLine(erlang.Sample(rand).ToString());
      }
      using (var file = System.IO.File.CreateText("distribution.csv")) {
        file.Write(sb.ToString());
      }
      Assert.True(System.IO.File.Exists("distribution.csv"));
    }
  }
}