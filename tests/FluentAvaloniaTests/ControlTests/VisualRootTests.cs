using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Xunit;

namespace FluentAvaloniaTests.ControlTests;

public class VisualRootTests
{
    [AvaloniaFact]
    public void AppWindowRegistersAttachedTitleBarControl()
    {
        var button = new Button();
        var window = new AppWindow
        {
            Width = 400,
            Height = 300,
            Content = button
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            AppWindow.SetAllowInteractionInTitleBar(button, true);

            var field = typeof(AppWindow).GetField(
                "_excludeHitTestList",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var controls = (IEnumerable<WeakReference<Control>>)field!.GetValue(window)!;

            Assert.Contains(controls, reference =>
                reference.TryGetTarget(out var target) && ReferenceEquals(target, button));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FAComboBoxSubscribesToItsOwningTopLevelWhenOpened()
    {
        var comboBox = new FAComboBox
        {
            Width = 200,
            ItemsSource = new[] { "One", "Two" },
            SelectedIndex = 0
        };
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = comboBox
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            comboBox.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();

            var field = typeof(FAComboBox).GetField(
                "_subscriptionsOnOpen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object subscriptions = field!.GetValue(comboBox)!;
            int count = (int)subscriptions.GetType().GetProperty(nameof(ICollection<object>.Count))!
                .GetValue(subscriptions)!;
            int expected = comboBox.GetVisualAncestors().OfType<Control>().Count() + 2;

            Assert.Equal(expected, count);
        }
        finally
        {
            comboBox.IsDropDownOpen = false;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TargetOnlyTeachingTipResolvesOwningTopLevel()
    {
        var target = new Button { Content = "Target" };
        var teachingTip = new TeachingTip
        {
            Target = target,
            Title = "Tip",
            CloseButtonContent = "Close"
        };
        var window = new Window
        {
            Width = 500,
            Height = 350,
            Content = target
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            target.Focus();

            var hostMethod = typeof(TeachingTip).GetMethod(
                "GetHostTopLevel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var rootField = typeof(TeachingTip).GetField(
                "_rootElement",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var f6Method = typeof(TeachingTip).GetMethod(
                "HandleF6Clicked",
                BindingFlags.Instance | BindingFlags.NonPublic);

            rootField!.SetValue(teachingTip, target);

            Assert.Same(window, hostMethod!.Invoke(teachingTip, null));
            Assert.False((bool)f6Method!.Invoke(teachingTip, new object[] { false })!);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FrameTracksOwningTopLevelForBackRequests()
    {
        var frame = new Frame();
        frame.Navigate(typeof(FramePageOne));
        frame.Navigate(typeof(FramePageTwo));
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = frame
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var field = typeof(Frame).GetField(
                "_topLevel",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(window, field!.GetValue(frame));
            window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
            Assert.IsType<FramePageOne>(frame.Content);
        }
        finally
        {
            window.Close();
        }

    }

    [AvaloniaFact]
    public void DialogHostMeasuresAgainstOwningTopLevel()
    {
        var host = new DialogHost();
        var window = new Window
        {
            Width = 420,
            Height = 280,
            Content = host
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            host.Measure(Size.Infinity);

            Assert.Equal(window.ClientSize, host.DesiredSize);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task AttachedTaskDialogUsesOwningTopLevel()
    {
        var dialog = new TaskDialog
        {
            Header = "Validation",
            Content = "TaskDialog owner validation"
        };
        dialog.Buttons.Add(TaskDialogButton.OKButton);
        var window = new Window
        {
            Width = 420,
            Height = 280,
            Content = dialog
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            dialog.Opened += (_, _) => dialog.Hide(TaskDialogStandardResult.OK);

            object result = await dialog.ShowAsync(showHosted: true);

            Assert.Equal(TaskDialogStandardResult.OK, result);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TeachingTipRetargetsPopupToTheTargetsTopLevel()
    {
        var firstTarget = new Button();
        var secondTarget = new Button();
        var firstWindow = new Window { Content = firstTarget };
        var secondWindow = new Window { Content = secondTarget };
        var teachingTip = new TeachingTip { Target = firstTarget };

        try
        {
            firstWindow.Show();
            secondWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var createPopup = typeof(TeachingTip).GetMethod(
                "CreateNewPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var popupField = typeof(TeachingTip).GetField(
                "_popup",
                BindingFlags.Instance | BindingFlags.NonPublic);

            createPopup!.Invoke(teachingTip, null);
            var popup = (Avalonia.Controls.Primitives.Popup)popupField!.GetValue(teachingTip)!;
            Assert.Same(firstWindow, popup.PlacementTarget);

            teachingTip.Target = secondTarget;

            Assert.Same(secondWindow, popup.PlacementTarget);
        }
        finally
        {
            firstWindow.Close();
            secondWindow.Close();
        }
    }

    [AvaloniaFact]
    public void TeachingTipRetargetsPopupWhenTheSameTargetMovesWindows()
    {
        var target = new Button();
        var firstWindow = new Window { Content = target };
        var secondWindow = new Window();
        var teachingTip = new TeachingTip { Target = target };

        try
        {
            firstWindow.Show();
            secondWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var createPopup = typeof(TeachingTip).GetMethod(
                "CreateNewPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var createLightDismissPopup = typeof(TeachingTip).GetMethod(
                "CreateLightDismissIndiatorPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var popupField = typeof(TeachingTip).GetField(
                "_popup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var lightDismissPopupField = typeof(TeachingTip).GetField(
                "_lightDismissIndicatorPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);

            createPopup!.Invoke(teachingTip, null);
            createLightDismissPopup!.Invoke(teachingTip, null);
            var popup = (Avalonia.Controls.Primitives.Popup)popupField!.GetValue(teachingTip)!;
            var lightDismissPopup =
                (Avalonia.Controls.Primitives.Popup)lightDismissPopupField!.GetValue(teachingTip)!;
            Assert.Same(firstWindow, popup.PlacementTarget);
            Assert.Same(firstWindow, lightDismissPopup.PlacementTarget);

            firstWindow.Content = null;
            Dispatcher.UIThread.RunJobs();
            secondWindow.Content = target;
            Dispatcher.UIThread.RunJobs();

            Assert.Same(secondWindow, popup.PlacementTarget);
            Assert.Same(secondWindow, lightDismissPopup.PlacementTarget);
        }
        finally
        {
            firstWindow.Close();
            secondWindow.Close();
        }
    }

    [AvaloniaFact]
    public void TeachingTipClosesBeforeCrossWindowRetarget()
    {
        FAUISettings.SetAnimationsEnabledAtAppLevel(false);
        var firstTarget = new Button();
        var secondTarget = new Button();
        var thirdTarget = new Button();
        var firstWindow = new Window { Content = firstTarget };
        var secondWindow = new Window { Content = secondTarget };
        var thirdWindow = new Window { Content = thirdTarget };
        var teachingTip = new TeachingTip { Target = firstTarget };

        try
        {
            firstWindow.Show();
            secondWindow.Show();
            thirdWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var ignoreOpenChangeField = typeof(TeachingTip).GetField(
                "_ignoreNextIsOpenChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var createPopup = typeof(TeachingTip).GetMethod(
                "CreateNewPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var popupField = typeof(TeachingTip).GetField(
                "_popup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            ignoreOpenChangeField!.SetValue(teachingTip, true);
            teachingTip.IsOpen = true;
            createPopup!.Invoke(teachingTip, null);
            var popup = (Avalonia.Controls.Primitives.Popup)popupField!.GetValue(teachingTip)!;
            popup.Child = new Border();
            popup.IsOpen = true;
            Dispatcher.UIThread.RunJobs();

            bool closingRaised = false;
            teachingTip.Closing += (_, _) => closingRaised = true;

            teachingTip.Target = secondTarget;
            teachingTip.Target = thirdTarget;

            Assert.True(popup.IsOpen);
            Assert.Same(firstWindow, popup.PlacementTarget);

            Dispatcher.UIThread.RunJobs();

            Assert.True(closingRaised);
            Assert.False(teachingTip.IsOpen);
            Assert.False(popup.IsOpen);
            Assert.Same(thirdWindow, popup.PlacementTarget);
        }
        finally
        {
            FAUISettings.SetAnimationsEnabledAtAppLevel(true);
            firstWindow.Close();
            secondWindow.Close();
            thirdWindow.Close();
        }
    }

    [AvaloniaFact]
    public void TeachingTipCancelledRetargetRemainsOpen()
    {
        FAUISettings.SetAnimationsEnabledAtAppLevel(false);
        var firstTarget = new Button();
        var secondTarget = new Button();
        var firstWindow = new Window { Content = firstTarget };
        var secondWindow = new Window { Content = secondTarget };
        var teachingTip = new TeachingTip { Target = firstTarget };

        try
        {
            firstWindow.Show();
            secondWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var ignoreOpenChangeField = typeof(TeachingTip).GetField(
                "_ignoreNextIsOpenChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var createPopup = typeof(TeachingTip).GetMethod(
                "CreateNewPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var popupField = typeof(TeachingTip).GetField(
                "_popup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var retargetField = typeof(TeachingTip).GetField(
                "_retargetAfterClose",
                BindingFlags.Instance | BindingFlags.NonPublic);
            ignoreOpenChangeField!.SetValue(teachingTip, true);
            teachingTip.IsOpen = true;
            createPopup!.Invoke(teachingTip, null);
            var popup = (Avalonia.Controls.Primitives.Popup)popupField!.GetValue(teachingTip)!;
            popup.Child = new Border();
            popup.IsOpen = true;
            Dispatcher.UIThread.RunJobs();

            teachingTip.Closing += (_, args) => args.Cancel = true;
            teachingTip.Target = secondTarget;
            Dispatcher.UIThread.RunJobs();

            Assert.True(teachingTip.IsOpen);
            Assert.True(popup.IsOpen);
            Assert.Same(firstTarget, teachingTip.Target);
            Assert.Same(firstWindow, popup.PlacementTarget);
            Assert.False((bool)retargetField!.GetValue(teachingTip)!);
        }
        finally
        {
            FAUISettings.SetAnimationsEnabledAtAppLevel(true);
            firstWindow.Close();
            secondWindow.Close();
        }
    }

    [AvaloniaFact]
    public void TeachingTipCancelledRetargetRestoresNullTarget()
    {
        FAUISettings.SetAnimationsEnabledAtAppLevel(false);
        var teachingTip = new TeachingTip();
        var secondTarget = new Button();
        var firstWindow = new Window
        {
            Content = new Panel { Children = { teachingTip } }
        };
        var secondWindow = new Window { Content = secondTarget };

        try
        {
            firstWindow.Show();
            secondWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var ignoreOpenChangeField = typeof(TeachingTip).GetField(
                "_ignoreNextIsOpenChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var createPopup = typeof(TeachingTip).GetMethod(
                "CreateNewPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var popupField = typeof(TeachingTip).GetField(
                "_popup",
                BindingFlags.Instance | BindingFlags.NonPublic);

            ignoreOpenChangeField!.SetValue(teachingTip, true);
            teachingTip.IsOpen = true;
            createPopup!.Invoke(teachingTip, null);
            var popup = (Avalonia.Controls.Primitives.Popup)popupField!.GetValue(teachingTip)!;
            popup.Child = new Border();
            popup.IsOpen = true;
            Dispatcher.UIThread.RunJobs();

            teachingTip.Closing += (_, args) => args.Cancel = true;
            teachingTip.Target = secondTarget;
            Dispatcher.UIThread.RunJobs();

            Assert.Null(teachingTip.Target);
            Assert.True(teachingTip.IsOpen);
            Assert.True(popup.IsOpen);
            Assert.Same(firstWindow, popup.PlacementTarget);
        }
        finally
        {
            FAUISettings.SetAnimationsEnabledAtAppLevel(true);
            firstWindow.Close();
            secondWindow.Close();
        }
    }

    [AvaloniaFact]
    public void TeachingTipRetriesRetargetAfterOpeningBecomesIdle()
    {
        FAUISettings.SetAnimationsEnabledAtAppLevel(false);
        var firstTarget = new Button();
        var secondTarget = new Button();
        var firstWindow = new Window { Content = firstTarget };
        var secondWindow = new Window { Content = secondTarget };
        var teachingTip = new TeachingTip { Target = firstTarget };

        try
        {
            firstWindow.Show();
            secondWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var ignoreOpenChangeField = typeof(TeachingTip).GetField(
                "_ignoreNextIsOpenChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var createPopup = typeof(TeachingTip).GetMethod(
                "CreateNewPopup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var popupField = typeof(TeachingTip).GetField(
                "_popup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var idleField = typeof(TeachingTip).GetField(
                "_isIdle",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var setIdleMethod = typeof(TeachingTip).GetMethod(
                "SetIsIdle",
                BindingFlags.Instance | BindingFlags.NonPublic);

            ignoreOpenChangeField!.SetValue(teachingTip, true);
            teachingTip.IsOpen = true;
            createPopup!.Invoke(teachingTip, null);
            var popup = (Avalonia.Controls.Primitives.Popup)popupField!.GetValue(teachingTip)!;
            popup.Child = new Border();
            popup.IsOpen = true;
            Dispatcher.UIThread.RunJobs();

            idleField!.SetValue(teachingTip, false);
            teachingTip.Target = secondTarget;
            Dispatcher.UIThread.RunJobs();

            Assert.True(teachingTip.IsOpen);
            Assert.True(popup.IsOpen);
            Assert.Same(firstWindow, popup.PlacementTarget);

            setIdleMethod!.Invoke(teachingTip, new object[] { true });
            Dispatcher.UIThread.RunJobs();

            Assert.False(teachingTip.IsOpen);
            Assert.False(popup.IsOpen);
            Assert.Same(secondWindow, popup.PlacementTarget);
        }
        finally
        {
            FAUISettings.SetAnimationsEnabledAtAppLevel(true);
            firstWindow.Close();
            secondWindow.Close();
        }
    }

    private sealed class FramePageOne : UserControl
    {
    }

    private sealed class FramePageTwo : UserControl
    {
    }
}
