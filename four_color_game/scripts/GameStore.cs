using FourColors;
using Godot;
using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Nakama;
using Newtonsoft.Json.Linq;

public partial class GameStore : Control
{

    [Export] public PackedScene popoutpaymentscene;

    private TabContainer TabContainer1;
    private Panel bg_panel;
    private AudioStreamPlayer bgm;
    private AudioStreamPlayer sfxm;

    private AcceptDialog autoMessageBox;
    private Label PointLabel, PurchaseWindow;
    public bool purchasepressed = false;

    public override void _Ready()
	{

        bgm = (AudioStreamPlayer)FindChild("BGM");
        sfxm = (AudioStreamPlayer)FindChild("SFXM");
        PointLabel = (Label)FindChild("PointLabel");
        PurchaseWindow = (Label)FindChild("PurchaseWindow");

        var stream = GD.Load<AudioStream>($"res://art/4_Color_Game/Music/Piki - Kitty (freetouse.com).mp3");
        if (stream != null)
        {
            bgm.Stream = stream;
            bgm.VolumeDb = -10;
            if (NakamaSingleton.Instance.BGMPlay)
                bgm.Play();
            ((AudioStreamMP3)bgm.Stream).Loop = true;
        }

        TabContainer1 = (TabContainer)FindChild("TabContainer");
        TabContainer1.TabChanged += TabContainer1_TabChanged;

        UpdateUI();

        autoMessageBox = (AcceptDialog)FindChild("windec_c");
        autoCloseTimer = (Timer)FindChild("Timer");
        autoCloseTimer.Timeout += () =>
        {
            autoMessageBox.Hide();
        };

        NakamaSingleton.Instance.UpdateSaveData();

    }

    private void TabContainer1_TabChanged(long tab)
    {
		LoggerManager.Info($"Tab1 Change to {TabContainer1.GetTabTitle((int)tab)}");
    }

    public override void _Process(double delta)
	{
        PointLabel.Text = $"Accumulated Points : {NakamaSingleton.Instance.SD.Points} ";
    }

    public void UpdateUI()
    {
        GameLogic.SetGameSaved(NakamaSingleton.Instance.SD);
        var keys = NakamaSingleton.Instance.SD.BG.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            var value = NakamaSingleton.Instance.SD.BG[key];
            if (value.Purchase == true)
            {
                ((Label)FindChild($"Price{i}")).Text = "Owned";
                ((Button)FindChild($"Purchase{i}")).Disabled = (bool)value.Purchase;
            }

            ((Button)FindChild($"Equip{i}")).Disabled = !(bool)value.Purchase;
            ((Button)FindChild($"Equip{i}")).ButtonPressed = (bool)value.Equip;
        }

        NakamaSingleton.Instance.UpdateSaveData();

        bg_panel = (Panel)FindChild("Panel");
        var stylebox = bg_panel.GetThemeStylebox("panel") as StyleBoxTexture;
        if (stylebox != null)
        {
            stylebox = (StyleBoxTexture)stylebox.Duplicate();
            bg_panel.AddThemeStyleboxOverride("panel", stylebox);

            stylebox.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Background/{NakamaSingleton.Instance.BGThemeEquiped}.png");
        }

        GameLogic.SetGameSaved(NakamaSingleton.Instance.SD);
        NakamaSingleton.Instance.UpdateSaveData();
    }


    private void _on_back_button_pressed()
    {
        LoggerManager.Info("back button pressed");
        bgm.Stop();
        sfxm.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }

    private void _on_donate_pressed()
    {
        OS.ShellOpen("https://ko-fi.com/hendylim");
    }

    private Timer autoCloseTimer;
    private float remainingMs;
    private float elapsedMs;
    private float updateIntervalMs = 50f; // update every 50ms

    public void ShowAutoMessage(string message, int durationMs = 2000)
    {
        remainingMs = durationMs;
        elapsedMs = 0;

        // Lazy create the timer
        if (autoCloseTimer == null)
        {
            autoCloseTimer = new Timer();
            autoCloseTimer.OneShot = false;
            autoCloseTimer.WaitTime = updateIntervalMs / 1000f;
            autoCloseTimer.Timeout += () => UpdateCountdown(message);
            AddChild(autoCloseTimer);
        }

        // Hide OK button if it's AcceptDialog
        if (autoMessageBox is AcceptDialog dialog)
        {
            dialog.GetOkButton()?.Hide();
        }

        // Initial display
        UpdateCountdown(message);
        autoMessageBox.PopupCentered();
        autoCloseTimer.Start();
    }

    private void UpdateCountdown(string baseMessage)
    {
        elapsedMs += updateIntervalMs;
        int timeLeft = Mathf.Max(0, (int)(remainingMs - elapsedMs));

        string display = $"{baseMessage}\n";// Closing in {timeLeft} ms";

        if (autoMessageBox.HasNode("MessageLabel"))
        {
            var label = autoMessageBox.GetNode<Label>("MessageLabel");
            label.Text = display;
        }
        else
        {
            autoMessageBox.DialogText = display;
        }

        if (timeLeft <= 0)
        {
            autoMessageBox.Hide();
            autoCloseTimer.Stop();
        }
    }

    /// <summary>
    /// BackGround Purchase and Equip
    /// </summary>
	private void _on_purchase_pressed()
	{
        
	}
    private void _on_equip_pressed()
    {
        NakamaSingleton.Instance.SD.BG[NakamaSingleton.Instance.BGThemeEquiped].Equip = false;
        NakamaSingleton.Instance.SD.BG["green_BG"].Equip = !NakamaSingleton.Instance.SD.BG["green_BG"].Equip;
        //((Button)FindChild($"Equip0")).ButtonPressed = NakamaSingleton.Instance.SD.BG["green_BG"].Equip;
        UpdateUI();
    }
    private void _on_purchase_pressed1()
    {
        if (NakamaSingleton.Instance.SD.Points < int.Parse(((Label)FindChild($"Price1")).Text))
        {
            ShowAutoMessage("You do not have enough point", 3000);
            return;
        }

        NakamaSingleton.Instance.SD.Points -= int.Parse(((Label)FindChild($"Price1")).Text);
        //((Label)FindChild($"Price1")).Text = "owned";
        NakamaSingleton.Instance.SD.BG["beehive1"].Purchase= true;
        //((Button)FindChild($"Equip1")).Disabled = false;
        UpdateUI();
    }
    private void _on_equip_pressed1()
    {
        if (!NakamaSingleton.Instance.SD.BG["beehive1"].Purchase)
        {
            ShowAutoMessage("You do not owned this", 3000);
            return;
        }

        NakamaSingleton.Instance.SD.BG[NakamaSingleton.Instance.BGThemeEquiped].Equip = false;
        NakamaSingleton.Instance.SD.BG["beehive1"].Equip = !NakamaSingleton.Instance.SD.BG["beehive1"].Equip;
        //((Button)FindChild($"Equip1")).ButtonPressed = NakamaSingleton.Instance.SD.BG["beehive1"].Equip;
        UpdateUI();
    }
    private void _on_purchase_pressed2()
    {
        if (NakamaSingleton.Instance.SD.Points < int.Parse(((Label)FindChild($"Price2")).Text))
        {
            ShowAutoMessage("You do not have enough point", 3000);
            return;
        }

        NakamaSingleton.Instance.SD.Points -= int.Parse(((Label)FindChild($"Price2")).Text);
        //((Label)FindChild($"Price2")).Text = "owned";
        NakamaSingleton.Instance.SD.BG["maze1"].Purchase = true;
        //((Button)FindChild($"Equip2")).Disabled = false;
        UpdateUI();

    }
    private void _on_equip_pressed2()
    {
        if (!NakamaSingleton.Instance.SD.BG["maze1"].Purchase)
        {
            ShowAutoMessage("You do not owned this", 3000);
            return;
        }
        NakamaSingleton.Instance.SD.BG[NakamaSingleton.Instance.BGThemeEquiped].Equip = false;
        NakamaSingleton.Instance.SD.BG["maze1"].Equip = !NakamaSingleton.Instance.SD.BG["maze1"].Equip;
        //((Button)FindChild($"Equip2")).ButtonPressed = NakamaSingleton.Instance.SD.BG["maze1"].Equip;
        UpdateUI();
    }
    private void _on_purchase_pressed3()
    {
        if (NakamaSingleton.Instance.SD.Points < int.Parse(((Label)FindChild($"Price3")).Text))
        {
            ShowAutoMessage("You do not have enough point", 3000);
            return;
        }

        NakamaSingleton.Instance.SD.Points -= int.Parse(((Label)FindChild($"Price3")).Text);
        //((Label)FindChild($"Price3")).Text = "owned";
        NakamaSingleton.Instance.SD.BG["MenuBG"].Purchase = true;
        //((Button)FindChild($"Equip3")).Disabled = false;
        UpdateUI();
    }
    private void _on_equip_pressed3()
    {
        if (!NakamaSingleton.Instance.SD.BG["MenuBG"].Purchase)
        {
            ShowAutoMessage("You do not owned this", 3000);
            return;
        }
        NakamaSingleton.Instance.SD.BG[NakamaSingleton.Instance.BGThemeEquiped].Equip = false;
        NakamaSingleton.Instance.SD.BG["MenuBG"].Equip = !NakamaSingleton.Instance.SD.BG["MenuBG"].Equip;
        //((Button)FindChild($"Equip3")).ButtonPressed = NakamaSingleton.Instance.SD.BG["MenuBG"].Equip;
        UpdateUI();
    }
    private void _on_purchase_pressed4()
    {
        if (NakamaSingleton.Instance.SD.Points < int.Parse(((Label)FindChild($"Price4")).Text))
        {
            ShowAutoMessage("You do not have enough point", 3000);
            return;
        }

        NakamaSingleton.Instance.SD.Points -= int.Parse(((Label)FindChild($"Price4")).Text);
        //((Label)FindChild($"Price4")).Text = "owned";
        NakamaSingleton.Instance.SD.BG["checkboard1"].Purchase = true;
        //((Button)FindChild($"Equip4")).Disabled = false;
        UpdateUI();
    }
    private void _on_equip_pressed4()
    {
        if (!NakamaSingleton.Instance.SD.BG["checkboard1"].Purchase)
        {
            ShowAutoMessage("You do not owned this", 3000);
            return;
        }
        NakamaSingleton.Instance.SD.BG[NakamaSingleton.Instance.BGThemeEquiped].Equip = false;
        NakamaSingleton.Instance.SD.BG["checkboard1"].Equip = !NakamaSingleton.Instance.SD.BG["checkboard1"].Equip;
        //((Button)FindChild($"Equip4")).ButtonPressed = NakamaSingleton.Instance.SD.BG["checkboard1"].Equip;
        UpdateUI();
    }
    private void _on_purchase_pressed5()
    {
        if (NakamaSingleton.Instance.SD.Points < int.Parse(((Label)FindChild($"Price5")).Text))
        {
            ShowAutoMessage("You do not have enough point", 3000);
            return;
        }

        NakamaSingleton.Instance.SD.Points -= int.Parse(((Label)FindChild($"Price5")).Text);
        //((Label)FindChild($"Price5")).Text = "owned";
        NakamaSingleton.Instance.SD.BG["wood1"].Purchase = true;
        //((Button)FindChild($"Equip5")).Disabled = false;
        UpdateUI();
    }
    private void _on_equip_pressed5()
    {
        if (!NakamaSingleton.Instance.SD.BG["wood1"].Purchase)
        {
            ShowAutoMessage("You do not owned this", 3000);
            return;
        }
        NakamaSingleton.Instance.SD.BG[NakamaSingleton.Instance.BGThemeEquiped].Equip = false;
        NakamaSingleton.Instance.SD.BG["wood1"].Equip = !NakamaSingleton.Instance.SD.BG["wood1"].Equip;
        //((Button)FindChild($"Equip5")).ButtonPressed = NakamaSingleton.Instance.SD.BG["wood1"].Equip;
        UpdateUI();
    }

    /// <summary>
    /// Points Purchase and Equip
    /// </summary>
    /// 

    private void _on_purchase_point()
    {
        _ = PurchasePointAsync();
    }
    private async Task PurchasePointAsync()
    {
        if (purchasepressed)
            return;
        purchasepressed = true;
        // $1
        //await GetCheckoutUrlAndOpen("P1_10000_", "price_1SfjVjBV5XpwFIyaZv8GxMUd");
        await GetCheckoutUrlAndOpen("P1_10000_", "price_1SfiAfB1iRqqupXsxMOB37qC");
    }
    private void _on_purchase_point2()
    {
        _ = PurchasePoint2Async();
    }
    private async Task PurchasePoint2Async()
    {
        if (purchasepressed)
            return;
        purchasepressed = true;
        // $1.5
        //await GetCheckoutUrlAndOpen("P1.5_20000_", "price_1SgQDoBV5XpwFIyaa29uu3SJ");
        await GetCheckoutUrlAndOpen("P1.5_20000_", "price_1SfiTcB1iRqqupXstbUf8amf");
    }

    private void _on_purchase_point3()
    {
        _ = PurchasePoint3Async();
    }
    private async Task PurchasePoint3Async()
    {
        if (purchasepressed)
            return;
        purchasepressed = true;
        // $2
        //await GetCheckoutUrlAndOpen("P2_30000_", "price_1SgQDoBV5XpwFIyaOTxLRLqF");
        await GetCheckoutUrlAndOpen("P2_30000_", "price_1SfiTmB1iRqqupXstWZ0w3Ch");
    }

    private void _on_purchase_point4()
    {
        _ = PurchasePoint4Async();
    }
    private async Task PurchasePoint4Async()
    {
        if (purchasepressed)
            return;
        purchasepressed = true;
        // $3
        //await GetCheckoutUrlAndOpen("P3_50000_", "price_1SgQDoBV5XpwFIyadhaFXun2");
        await GetCheckoutUrlAndOpen("P3_50000_", "price_1SfiU6B1iRqqupXsn7WamAHS");
    }

    private void _on_purchase_point5()
    {
        _ = PurchasePoint5Async();
    }
    private async Task PurchasePoint5Async()
    {
        if (purchasepressed)
            return;
        purchasepressed = true;
        // $5
        //await GetCheckoutUrlAndOpen("P5_100000_", "price_1SgQDoBV5XpwFIyaSnMBcCTi");
        await GetCheckoutUrlAndOpen("P5_100000_", "price_1SfiVlB1iRqqupXsbKkDJUAy");
    }


    private static readonly System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

    private async Task GetCheckoutUrlAndOpen(string token="",string priceID= "price_1Rs2VtQ2uLIvcn7YRtBim8JQ")
    {
        token = token + GameLogic.GenerateToken();
        NakamaSingleton.Instance.GameToken = token;
        string url = $"https://renderserver-2hxj.onrender.com/create_checkout?token={NakamaSingleton.Instance.GameToken}&product_id=ss&price_id={priceID}";
        LoggerManager.Info($"url POST {url}");

        // Loading window
        PurchaseWindow.Visible = true;

        try
        {

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            LoggerManager.Info($"✅ Server response: {responseBody}");

            JObject json = JObject.Parse(responseBody);
            string checkoutUrl = json["checkout_url"]?.ToString();
            PurchaseWindow.Visible = false;
            if (!string.IsNullOrEmpty(checkoutUrl))
            {
                LoggerManager.Info($"🌐 Opening Stripe Checkout:{checkoutUrl}");
                OS.ShellOpen(checkoutUrl); 
            }
            else
            {
                LoggerManager.Info("❌ checkout_url not found in response.");
                return;
            }

            LoggerManager.Info("Waiting for payment...");
            PurchasePopup popuppaymentconfirm = (PurchasePopup)popoutpaymentscene.Instantiate();
            popuppaymentconfirm.ParentStore = this;
            AddChild(popuppaymentconfirm);
        }
        catch (Exception e)
        {
            LoggerManager.Error($"❌ Error during request: {e.Message}");
        }
    }

}
