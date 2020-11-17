/*
   Based on https://github.com/brianlagunas/PrismOutlook/blob/master/PrismOutlook.Core/DependentViewAttribute.cs
   MIT License, Copyright (c) 2019 Brian Lagunas. See https://github.com/brianlagunas/PrismOutlook/blob/master/LICENSE
*/

using System;

namespace VideoScriptEditor.PrismExtensions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependentViewAttribute : Attribute
    {
        public string Region { get; set; }

        public Type Type { get; set; }

        public DependentViewAttribute(string region, Type type)
        {
            Region = region ?? throw new ArgumentNullException(nameof(region));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
