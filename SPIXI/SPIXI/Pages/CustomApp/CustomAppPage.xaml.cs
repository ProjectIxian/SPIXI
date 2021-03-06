﻿using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Linq;
using System.Text;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomAppPage : SpixiContentPage
    {
        public string appId = null;
        public byte[] sessionId = null; // App session ID

        public byte[] myRequestAddress = null; // which address the app request was sent to
        public byte[] requestedByAddress = null; // which address sent the app request to us

        public byte[] hostUserAddress = null; // address of the user that initiated the app
        private byte[][] userAddresses = null; // addresses of all users connected to/using the app

        public bool accepted = false;
        public long requestReceivedTimestamp = 0;


        public CustomAppPage(string app_id, byte[] host_user_address, byte[][] user_addresses, string app_entry_point)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            sessionId = Guid.NewGuid().ToByteArray();

            appId = app_id;

            hostUserAddress = host_user_address;
            userAddresses = user_addresses;

            // Load the app entry point
            var source = new UrlWebViewSource();
            source.Url = "file://" + app_entry_point;
            webView.Source = source;

            requestReceivedTimestamp = Clock.getTimestamp();
        }



        public override void recalculateLayout()
        {
            ForceLayout();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);
            e.Cancel = true;

            if (onNavigatingGlobal(current_url))
            {
                return;
            }

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                onBack();
            }
            else if (current_url.StartsWith("ixian:data", StringComparison.Ordinal))
            {
                onNetworkData(current_url.Substring(10));
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            // Execute timer-related functionality immediately
            updateScreen();
        }

        // Executed every second
        public override void updateScreen()
        {

        }

        private void onNetworkData(string data)
        {
            foreach(byte[] address in userAddresses)
            {
                Friend f = FriendList.getFriend(address);
                if(f != null)
                {
                    // TODO TODO TODO probably a different encoding should be used for data
                    StreamProcessor.sendAppData(f, sessionId, UTF8Encoding.UTF8.GetBytes(data));
                }else
                {
                    Logging.error("Friend {0} does not exist in the friend list.", Base58Check.Base58CheckEncoding.EncodePlain(address));
                }
            }
        }

        private void onBack()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);
            Node.customAppManager.removeAppPage(sessionId);
        }

        public void networkDataReceive(byte[] sender_address, byte[] data)
        {
            // TODO TODO TODO probably a different encoding should be used for data
            Utils.sendUiCommand(webView, "networkData", UTF8Encoding.UTF8.GetString(data));
        }

        public void appRequestAcceptReceived(byte[] sender_address, byte[] data)
        {
            // TODO TODO TODO probably a different encoding should be used for data
            Utils.sendUiCommand(webView, "onRequestAccept", UTF8Encoding.UTF8.GetString(sender_address), UTF8Encoding.UTF8.GetString(data));
        }

        public void appRequestRejectReceived(byte[] sender_address, byte[] data)
        {
            // TODO TODO TODO probably a different encoding should be used for data
            Utils.sendUiCommand(webView, "onRequestReject", UTF8Encoding.UTF8.GetString(sender_address), UTF8Encoding.UTF8.GetString(data));
        }

        public void appEndSessionReceived(byte[] sender_address, byte[] data)
        {
            // TODO TODO TODO probably a different encoding should be used for data
            Utils.sendUiCommand(webView, "onAppEndSession", UTF8Encoding.UTF8.GetString(sender_address), UTF8Encoding.UTF8.GetString(data));
        }

        public bool hasUser(byte[] user)
        {
            if(userAddresses.Select(x => x.SequenceEqual(user)) != null)
            {
                return true;
            }
            return false;
        }

        protected override bool OnBackButtonPressed()
        {
            onBack();

            return true;
        }
    }
}