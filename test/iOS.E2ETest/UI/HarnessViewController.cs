using System;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using iOS.E2ETest;
using Microsoft.WindowsAzure.MobileServices.TestFramework;
using MonoTouch.Dialog;
using UIKit;

namespace Microsoft.WindowsAzure.Mobile.iOS.Test
{
    public class HarnessViewController : DialogViewController, ITestReporter
    {
        public HarnessViewController() : base(UITableViewStyle.Grouped, null, pushing: true)
        {
            this.progress = new UIProgressView(new CGRect(0, 0, View.Bounds.Width, 5))
            {
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth
            };

            Root = new RootElement("Test Status") {
                new Section {
                    this.status,
                    new UIViewElement (null, this.progress, false),
    		        this.numbers
                }
            };

            AppDelegate.Harness.Reporter = this;
            Task.Factory.StartNew(AppDelegate.Harness.RunAsync);
        }

        public void StartRun(TestHarness harness)
        {
        }

        public void Progress(TestHarness harness)
        {
            InvokeOnMainThread(() =>
            {
                this.numbers.Value = String.Format("Passed: {0}  Failed: {1}", harness.Progress - harness.Failures, harness.Failures);
                ReloadData();

                float value = harness.Progress;
                int count = harness.Count;
                if (count > 0)
                    value = value / count;

                this.progress.Progress = value;
            });
        }

        public void EndRun(TestHarness harness)
        {
            InvokeOnMainThread(() => showAlert("Tests Complete", "All done!"));
        }

        public void StartGroup(TestGroup testGroup)
        {
            BeginInvokeOnMainThread(() =>
            {
                this.testsSection = new Section(testGroup.Name);
                Root.Add(this.testsSection);
            });
        }

        public void EndGroup(TestGroup testGroup)
        {

        }

        public void StartTest(TestMethod test)
        {
            this.testLog = new StringBuilder();
        }

        public void EndTest(TestMethod test)
        {
            string output = this.testLog.ToString();
            BeginInvokeOnMainThread(() =>
            {
                var element = new StyledStringElement(test.Name,
                    () => NavigationController.PushViewController(new TestViewController(test, output), true));

                if (!test.Passed)
                    element.TextColor = UIColor.Red;

                this.testsSection.Add(element);
            });
        }

        public void Log(string message)
        {
            this.testLog.AppendLine(message);
        }

        public void Error(string errorDetails)
        {
            this.testLog.AppendLine(errorDetails);
        }

        public void Status(string status)
        {
            InvokeOnMainThread(() =>
            {
                this.status.Caption = status;
                ReloadData();
            });
        }

        private void showAlert(string title, string message)
        {
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
            PresentViewController(alert, true, null);
        }

        private StringBuilder testLog;

        private Section testsSection;
        private readonly StyledStringElement status = new StyledStringElement(null, null, UITableViewCellStyle.Subtitle);
        private readonly StyledStringElement numbers = new StyledStringElement(null, null, UITableViewCellStyle.Subtitle);
        private readonly UIProgressView progress;
    }
}