using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace _03_05e;

public class MyMathPlugin
{
  [KernelFunction, Description("Take the square root of a number")]
  public static double Sqrt(
  [Description("The number to take a square root of")] double number1)
  {
    return Math.Sqrt(number1);
  }
}
