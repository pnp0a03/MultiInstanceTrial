using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Security.Cryptography;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// This code is generated from the Multi-Instance with Redirection project template.
// In addition to the SupportsMultipleInstances declaration in the package.appxmanifest,
// it also includes a Program.cs which defines a simple Main method - this is required
// if you want to use multi-instance activation redirection.

namespace MultiInstanceTrial
{
    sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        public AppInstance Instance;

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Instance = AppInstance.FindOrRegisterInstanceForKey(CryptographicBuffer.GenerateRandomNumber().ToString());

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }
                Window.Current.Content = rootFrame;
            }
            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                Window.Current.Activate();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = (ProtocolActivatedEventArgs)args;
                //string url = protocolArgs.Uri.AbsoluteUri;

                Instance = AppInstance.FindOrRegisterInstanceForKey(CryptographicBuffer.GenerateRandomNumber().ToString());

                if(null == rootFrame)
                {
                    rootFrame = new Frame();
                    Window.Current.Content = rootFrame;
                }

                if(rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), "");
                }
                Window.Current.Activate();
            }
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
