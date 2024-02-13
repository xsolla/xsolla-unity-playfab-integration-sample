using System;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;
using Xsolla.Core;

public class XsollaPlayfabSample : MonoBehaviour
{
    private void Start()
    {
        // Logging in anonymously
        LoginAnonymous(
            // Callback function invoked after successful login
            userId => {
                // Requesting Xsolla payment token
                GetXsollaPaymentToken(
                    userId, // PlayFab user ID received after login
                    "booster_max_1", // SKU of the product
                    orderData => {
                        // Creating Xsolla token and opening purchase UI
                        XsollaToken.Create(orderData.token);
                        XsollaWebBrowser.OpenPurchaseUI(orderData.token);

                        // Adding order for tracking
                        OrderTrackingService.AddOrderForTracking(
                            orderData.order_id,
                            true,
                            () => Debug.Log("Payment completed"),
                            onError => Debug.LogError(onError.errorMessage));
                    });
            });
    }

    private static void LoginAnonymous(Action<string> onSuccess)
    {
        // Logging in with custom ID
        PlayFabClientAPI.LoginWithCustomID(
            new LoginWithCustomIDRequest {
                CustomId = SystemInfo.deviceUniqueIdentifier, // Unique ID generated based on the device
                CreateAccount = true
            },
            result => {
                // Logging the result
                Debug.Log("Logged with playfab id: " + result.PlayFabId);

                // Invoking onSuccess callback with PlayFab ID
                onSuccess?.Invoke(result.PlayFabId);
            },
            error => { Debug.LogError(error.GenerateErrorReport()); }); // Handling login error
    }

    private static void GetXsollaPaymentToken(string userId, string sku, Action<OrderData> onSuccess)
    {
        // Creating request data for Xsolla payment token
        var tokenRequestData = new PaymentTokenRequestData {
            uid = userId, // User ID
            sku = sku, // Product SKU
            returnUrl = $"app://xpayment.{Application.identifier}" // Return URL
        };

        // Executing a function in the PlayFab cloud to get payment token
        PlayFabCloudScriptAPI.ExecuteFunction(
            new ExecuteFunctionRequest {
                FunctionName = "GetXsollaPaymentToken", // Name of Azure function
                FunctionParameter = tokenRequestData, // Data passed to the function
                GeneratePlayStreamEvent = false // Setting true if call should show up in PlayStream
            },
            result => {
                // Logging the result
                Debug.Log($"GetXsollaPaymentToken result: {result.FunctionResult}");

                // Parsing JSON result to OrderData object
                OrderData orderData = JsonUtility.FromJson<OrderData>(result.FunctionResult.ToString());

                // Invoking onSuccess callback with order data
                onSuccess?.Invoke(orderData);
            },
            error => Debug.LogError($"Error: {error.GenerateErrorReport()}")); // Handling error
    }

    // Class for payment token request data
    public class PaymentTokenRequestData
    {
        public string uid; // User ID
        public string sku; // Product SKU
        public string returnUrl; // Return URL
    }
}