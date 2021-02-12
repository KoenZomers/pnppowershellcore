﻿using PnP.Framework;
using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;

namespace PnP.PowerShell.Commands.Utilities
{
    public static class AzureAuthHelper
    {
        private static string CLIENTID = "1950a258-227b-4e31-a9cf-717495945fc2"; // Well-known Azure Management App Id
        internal static async Task<string> AuthenticateAsync(string tenantId, string username, SecureString password, AzureEnvironment azureEnvironment)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException($"{nameof(tenantId)} is required");
            }

            using (var authManager = PnP.Framework.AuthenticationManager.CreateWithCredentials(username, password, azureEnvironment))
            {
                return await authManager.GetAccessTokenAsync(new[] { "https://graph.microsoft.com/.default" });
            }
        }

        internal static string AuthenticateDeviceLogin(CancellationTokenSource cancellationTokenSource, CmdletMessageWriter messageWriter, bool noPopup, AzureEnvironment azureEnvironment, string clientId = "1950a258-227b-4e31-a9cf-717495945fc2")
        {
            try
            {
                using (var authManager = PnP.Framework.AuthenticationManager.CreateWithDeviceLogin(clientId, (result) =>
                {

                    if (Utilities.OperatingSystem.IsWindows() && !noPopup)
                    {
                        ClipboardService.SetText(result.UserCode);
                        messageWriter.WriteWarning($"Please login.\n\nWe opened a browser and navigated to {result.VerificationUrl}\n\nEnter code: {result.UserCode} (we copied this code to your clipboard)\n\nNOTICE: close the popup after you authenticated successfully to continue the process.");
                        BrowserHelper.GetWebBrowserPopup(result.VerificationUrl, "Please login", cancellationTokenSource: cancellationTokenSource, cancelOnClose: false);
                    }
                    else
                    {
                        messageWriter.WriteWarning(result.Message);
                    }
                    return Task.FromResult(0);
                }, azureEnvironment))
                {
                    authManager.ClearTokenCache();
                    try
                    {
                        return authManager.GetAccessTokenAsync(new string[] { "https://graph.microsoft.com/.default" }, cancellationTokenSource.Token).GetAwaiter().GetResult();
                    }
                    catch (Microsoft.Identity.Client.MsalException)
                    {
                        return null;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancellationTokenSource.Cancel();
            }
            return null;
        }

        internal static string AuthenticateInteractive(CancellationTokenSource cancellationTokenSource, CmdletMessageWriter messageWriter, bool noPopup, AzureEnvironment azureEnvironment)
        {
            try
            {
                using (var authManager = PnP.Framework.AuthenticationManager.CreateWithInteractiveLogin(CLIENTID, (url, port) =>
                {
                    BrowserHelper.OpenBrowserForInteractiveLogin(url, port, !noPopup, cancellationTokenSource);
                },
                successMessageHtml: $"You successfully authenticated with PnP PowerShell. Feel free to close this {(noPopup ? "tab" : "window")}.",
                failureMessageHtml: $"You did not authenticate with PnP PowerShell. Feel free to close this browser {(noPopup ? "tab" : "window")}."))
                {
                    authManager.ClearTokenCache();
                    try
                    {
                        return authManager.GetAccessTokenAsync(new string[] { "https://graph.microsoft.com/.default" }, cancellationTokenSource.Token).GetAwaiter().GetResult();
                    }
                    catch (Microsoft.Identity.Client.MsalException)
                    {
                        return null;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancellationTokenSource.Cancel();
            }
            return null;
        }
    }
}