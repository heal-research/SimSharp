using System;

namespace SimSharp {
  public static class Formatter {
    public static string Format12(double number, bool silenceNaN = true) {
      if (double.IsNaN(number)) return silenceNaN ? "            " : "     nan    ";
      if (number >= 1e7 || number <= -1e7) {
        if (number >= 1e100 || number <= -1e100) return string.Format("{0,12:0.####E+000}", number);
        return string.Format("{0,12:0.#####E+00}", number);
      }
      if (Math.Abs(number) < 1e-3) {
        if (number == 0) return "       0    ";
        if (Math.Abs(number) <= 1e-100) {
          return string.Format("{0,12:0.####E-000}", number);
        }
        return string.Format("{0,12:0.#####E-00}", number);
      }
      if (number == (int)number) return string.Format("{0,8}    ", (int)number);
      return string.Format("{0,12}", number.ToString("0.000"));
    }

    public static string Format15(double number, bool silenceNaN = true) {
      if (double.IsNaN(number)) return silenceNaN ? "               " : "        nan    ";
      if (number >= 1e10 || number <= -1e10) {
        if (number >= 1e100 || number <= -1e100) return string.Format("{0,15:0.#######E+000}", number);
        return string.Format("{0,15:0.########E+00}", number);
      }
      if (Math.Abs(number) < 1e-3) {
        if (number == 0) return "          0    ";
        if (Math.Abs(number) <= 1e-100) {
          return string.Format("{0,15:0.#######E-000}", number);
        }
        return string.Format("{0,15:0.########E-00}", number);
      }
      if (number == (int)number) return string.Format("{0,11}    ", (int)number);
      return string.Format("{0,15}", number.ToString("0.000"));
    }
  }
}
