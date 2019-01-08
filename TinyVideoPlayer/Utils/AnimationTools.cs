using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TinyVideoPlayer.Utils
{
    public static class AnimationTools
    {
        #region Properties

        public static DoubleAnimation FadeOutAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            FillBehavior = FillBehavior.Stop,
            BeginTime = TimeSpan.FromSeconds(0),
            Duration = new Duration(TimeSpan.FromSeconds(0.15))
        };

        public static DoubleAnimation FadeInAnim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            FillBehavior = FillBehavior.Stop,
            BeginTime = TimeSpan.FromSeconds(0),
            Duration = new Duration(TimeSpan.FromSeconds(0.15))
        };

        public static Storyboard StoryboardFadeOut { get; set; }

        public static Storyboard StoryboardFadeIn { get; set; }

        #endregion // Properties

        #region Constructors

        static AnimationTools()
        {
            StoryboardFadeIn = new Storyboard();
            StoryboardFadeIn.Children.Add(FadeInAnim);

            StoryboardFadeOut = new Storyboard();
            StoryboardFadeOut.Children.Add(FadeOutAnim);

            Storyboard.SetTargetProperty(FadeOutAnim, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTargetProperty(FadeInAnim, new PropertyPath(UIElement.OpacityProperty));
        }

        #endregion // Constructors

        #region Methods

        /// <summary>
        /// Fade out a control, opacity to 0.
        /// </summary>
        /// <param name="control">The control to hide.</param>
        public static void FadeOut(Control control)
        {
            Storyboard.SetTarget(FadeOutAnim, control);
            StoryboardFadeOut.Completed += delegate { control.Opacity = 0; control.Visibility = Visibility.Hidden; };
            StoryboardFadeOut.Begin();
        }

        /// <summary>
        /// Fade in a control, opacity to 1.
        /// </summary>
        /// <param name="control">The control to show.</param>
        public static void FadeIn(Control control)
        {
            Storyboard.SetTarget(FadeInAnim, control);
            StoryboardFadeIn.Completed += delegate { control.Opacity = 1;};
            control.Visibility = Visibility.Visible;
            StoryboardFadeIn.Begin();
        }

        #endregion // Methods
    }
}
