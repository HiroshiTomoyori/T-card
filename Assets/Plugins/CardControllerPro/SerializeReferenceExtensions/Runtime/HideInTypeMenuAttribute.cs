using System;

namespace MackySoft.SerializeReferenceExtensions
{

/// <summary>
/// An attribute that hides the type in the SubclassSelector.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class HideInTypeMenuAttribute : Attribute
{

}

}