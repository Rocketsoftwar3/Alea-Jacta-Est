using System;

namespace Alea_Jacta_Est.Services;

/// <summary>Marks a method as a card creator for random selection.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CardCreatorAttribute : Attribute
{
}
