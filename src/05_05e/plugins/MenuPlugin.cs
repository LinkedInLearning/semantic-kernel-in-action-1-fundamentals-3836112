using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace _05_05e;
public sealed class MenuPlugin
{
  [KernelFunction, Description("Provides today's the menu.")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
  public string GetMenu()
  {
    return @"
            Starters: Tom ka gay soup
            Main dish: Eye Steak with veggies
            Dessert: Mango with Rice
            ";
  }

  [KernelFunction, Description("Provides the price of the requested menu item.")]
  public string GetItemPrice(
      [Description("The name of the menu item.")]
            string menuItem)
  {
    return "CHF 19.99";
  }
}