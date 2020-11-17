/*
    Based on ContentControlRegionAdapter.cs, located at https://github.com/PrismLibrary/Prism/blob/master/src/Wpf/Prism.Wpf/Regions/ContentControlRegionAdapter.cs

    Prism is licensed under The MIT License (MIT), Copyright (c) .NET Foundation.
    The full text of the license is available at https://github.com/PrismLibrary/Prism/blob/master/LICENSE
*/

using Prism.Regions;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace VideoScriptEditor.PrismExtensions
{
    /// <summary>
    /// Adapter that creates a new <see cref="SingleActiveRegion"/> and monitors its
    /// active view to set it on the adapted <see cref="ContentPresenter"/>.
    /// </summary>
    public class ContentPresenterRegionAdapter : RegionAdapterBase<ContentPresenter>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ContentPresenterRegionAdapter"/>.
        /// </summary>
        /// <param name="regionBehaviorFactory">The factory used to create the region behaviors to attach to the created regions.</param>
        public ContentPresenterRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory)
        {
        }

        /// <summary>
        /// Adapts a <see cref="ContentPresenter"/> to a <see cref="IRegion"/>.
        /// </summary>
        /// <param name="region">The new region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        protected override void Adapt(IRegion region, ContentPresenter regionTarget)
        {
            if (regionTarget == null)
                throw new ArgumentNullException(nameof(regionTarget));

            bool contentIsSet = regionTarget.Content != null;
            contentIsSet = contentIsSet || (BindingOperations.GetBinding(regionTarget, ContentPresenter.ContentProperty) != null);

            if (contentIsSet)
                throw new InvalidOperationException("ContentPresenter's Content property is not empty. This control is being associated with a region, but the control is already bound to something else. If you did not explicitly set the control's Content property, this exception may be caused by a change in the value of the inherited RegionManager attached property.");

            region.ActiveViews.CollectionChanged += delegate
            {
                regionTarget.Content = region.ActiveViews.FirstOrDefault();
            };

            region.Views.CollectionChanged +=
                (sender, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (object activeView in region.ActiveViews)
                        {
                            region.Deactivate(activeView);
                        }

                        region.Activate(e.NewItems[0]);
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
    }
}
