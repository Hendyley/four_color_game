using FourColors;
using Godot;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class PurchasePopup : Node
{
    private Label PurchaseStatus;
    private Button PaymentButton;
    private float checkTimer = 0f;
    private float pollInterval = 3f; // check every 3 seconds
    private bool shouldCheck = true;
    private bool purchaseConfirmed = false;
    private float closeDelay = 5f; // seconds after purchase confirmed
    private float closeTimer = 0f;
    public GameStore ParentStore { get; set; }


    private static readonly System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

    public override void _Ready()
    {
        PurchaseStatus = (Label)FindChild("Purchase status2");
        PaymentButton = (Button)FindChild("btn");
        PurchaseStatus.Text = "🔍 Waiting for payment...";
    }

    public override void _Process(double delta)
    {
        if (purchaseConfirmed)
        {
            closeTimer += (float)delta;
            int remaining = Mathf.CeilToInt(closeDelay - closeTimer);
            PaymentButton.Text = $"Completed\r\nPayment ({remaining})";
            if (closeTimer >= closeDelay)
            {
                LoggerManager.Info("✅ Payment complete. Closing popup.");
                Match match = Regex.Match(NakamaSingleton.Instance.GameToken, @"^P[^_/]*_([^_/]+)[_/]");

                if (match.Success)
                {
                    string result = match.Groups[1].Value;
                    LoggerManager.Info($"Credit {result} points");
                    NakamaSingleton.Instance.SD.Points += int.Parse(result);
                    ParentStore?.UpdateUI();
                }
                NakamaSingleton.Instance.GameToken = "";
                ClosePopup();
            }
            return;
        }

        if (!shouldCheck)
            return;

        checkTimer += (float)delta;
        if (checkTimer >= pollInterval)
        {
            checkTimer = 0f;
            _ = CheckPurchaseStatus();
        }
    }

    public async Task CheckPurchaseStatus()
    {
        if (string.IsNullOrEmpty(NakamaSingleton.Instance.GameToken))
            return;

        string url = $"https://renderserver-2hxj.onrender.com/check_purchase?token={NakamaSingleton.Instance.GameToken}";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            LoggerManager.Info($"📦 Response: {json}");

            JObject data = JObject.Parse(json);
            bool purchase = data["purchase"]?.ToObject<bool>() ?? false;

            if (purchase)
            {
                string product = data["product"]?.ToString() ?? "Unknown";
                float amount = data["amount"]?.ToObject<float>() ?? 0f;
                PurchaseStatus.Text = $"✅ Purchased {product} (${amount})";
                purchaseConfirmed = true;
                shouldCheck = false;
                closeTimer = 0f;
            }
            else
            {
                PurchaseStatus.Text = "⌛ Payment pending...";
            }
        }
        catch (Exception e)
        {
            LoggerManager.Error($"❌ Error: {e.Message}");
            PurchaseStatus.Text = "⚠️ Error checking payment.";
        }
    }

    public void _on_btn_pressed()
    {
        if (purchaseConfirmed)
        {
            LoggerManager.Info("✅ Payment complete. Closing popup.");
            Match match = Regex.Match(NakamaSingleton.Instance.GameToken, @"^P[^_/]*_([^_/]+)[_/]");

            if (match.Success)
            {
                string result = match.Groups[1].Value;
                LoggerManager.Info($"Credit {result} points");
                NakamaSingleton.Instance.SD.Points += int.Parse(result);
                ParentStore?.UpdateUI();
            }
            NakamaSingleton.Instance.GameToken = "";
        }
        else
        {
            LoggerManager.Info("❌ Cancel clicked. Stopping checks and closing popup.");
            shouldCheck = false;
        }
        ClosePopup();
    }

    private void ClosePopup()
    {
        ParentStore.purchasepressed = false;
        GetParent().RemoveChild(this);
        QueueFree();
    }
}
