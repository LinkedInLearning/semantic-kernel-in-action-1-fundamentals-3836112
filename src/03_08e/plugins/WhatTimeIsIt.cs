using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace _03_08e;

public class WhatTimeIsIt
{
  [KernelFunction, Description("Get the current time")]
  public string Time(IFormatProvider? formatProvider = null) =>
      DateTimeOffset.Now.ToString("hh:mm:ss tt", formatProvider);
}
