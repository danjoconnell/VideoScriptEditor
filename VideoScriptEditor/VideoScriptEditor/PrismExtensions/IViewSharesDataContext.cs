/*
   Based on https://github.com/brianlagunas/PrismOutlook/blob/master/PrismOutlook.Core/ISupportDataContext.cs
   MIT License, Copyright (c) 2019 Brian Lagunas. See https://github.com/brianlagunas/PrismOutlook/blob/master/LICENSE
*/

namespace VideoScriptEditor.PrismExtensions
{
    public interface IViewSharesDataContext
    {
        object DataContext { get; set; }
    }
}
