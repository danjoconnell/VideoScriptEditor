/*
    Based on https://github.com/brianlagunas/PrismOutlook/blob/master/PrismOutlook/Core/Regions/XamRibbonRegionAdapter.cs
    MIT License, Copyright (c) 2019 Brian Lagunas. See https://github.com/brianlagunas/PrismOutlook/blob/master/LICENSE
*/

using Fluent;
using Prism.Regions;
using System.Collections.Specialized;

namespace VideoScriptEditor.PrismExtensions
{
    /// <summary>
    /// Adapter that creates a new <see cref="SingleActiveRegion"/> and monitors its
    /// active view to set it on the adapted <see cref="RibbonTabItem"/>.
    /// </summary>
    public class FluentRibbonTabItemRegionAdapter : RegionAdapterBase<RibbonTabItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FluentRibbonTabItemRegionAdapter"/>.
        /// </summary>
        /// <param name="regionBehaviorFactory">The factory used to create the region behaviors to attach to the created regions.</param>
        public FluentRibbonTabItemRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory)
        {
        }

        /// <summary>
        /// Adapts a <see cref="RibbonTabItem"/> to a <see cref="IRegion"/>.
        /// </summary>
        /// <param name="region">The new region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        protected override void Adapt(IRegion region, RibbonTabItem regionTarget)
        {
            region.Views.CollectionChanged += (sender, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var view in e.NewItems)
                        {
                            AddViewToRegion(view, regionTarget);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var view in e.OldItems)
                        {
                            RemoveViewFromRegion(view, regionTarget);
                        }
                        break;
                }
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="SingleActiveRegion"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="SingleActiveRegion"/>.</returns>
        protected override IRegion CreateRegion()
        {
            return new SingleActiveRegion();
        }

        static void AddViewToRegion(object view, RibbonTabItem ribbonTab)
        {
            if (view is RibbonGroupBox ribbonGroupBox && !ribbonTab.Groups.Contains(ribbonGroupBox))
            {
                ribbonTab.Groups.Add(ribbonGroupBox);
            }
        }

        static void RemoveViewFromRegion(object view, RibbonTabItem ribbonTab)
        {
            if (view is RibbonGroupBox ribbonGroupBox && ribbonTab.Groups.Contains(ribbonGroupBox))
            {
                ribbonTab.Groups.Remove(ribbonGroupBox);
            }
        }
    }
}
