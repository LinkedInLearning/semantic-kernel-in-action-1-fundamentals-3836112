using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace _04_03e;

public class WhatDateIsIt
{
  [KernelFunction, Description("Get the current date")]
  public string Date(IFormatProvider? formatProvider = null) =>
      DateTimeOffset.UtcNow.ToString("D", formatProvider);
}
