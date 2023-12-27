using Terminal.Gui;
using Tradeinator.Shared;

namespace Tradeinator.EventTester;

public class EventWindow : Window
{
    private PublisherExchange _exchange;
    private bool isConnected = false;
    
    
    public EventWindow()
    {
        
        Title = "Event Tester (Ctrl+Q to quit)";
        
        var top = AddExchangeConnectControls();
        var bottom = CreateEventInputs();

        bottom.Y = Pos.Bottom(top) - 1;
        
        Add(top, bottom);
    }

    private FrameView CreateEventInputs()
    {
        var fView = new FrameView();
        fView.Height = Dim.Fill();
        fView.Width = Dim.Fill();
        
        var eventTopicLabel = new Label()
        {
            Text = "Event Topic"
        };

        var eventTopicText = new TextField()
        {
            X = Pos.Right(eventTopicLabel) + 1,
            Width = Dim.Fill(),
            DesiredCursorVisibility = CursorVisibility.Vertical
        };

        var eventJsonLabel = new Label()
        {
            Text = "Event Json",
            Y = Pos.Bottom(eventTopicText) + 1,
            X = Pos.Left(eventTopicLabel)
        };
        
        
        var eventJsonText = new TextView()
        {
            
            Y = Pos.Bottom(eventJsonLabel) + 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(80)
            
        };
        
        
        var postButton = new Button()
        {
            Text = "Publish message",
            Y = Pos.Bottom(eventJsonText) + 1,
            X = Pos.Center()
        };
        
        postButton.Clicked += () =>
        {
            if (!isConnected)
            {
                MessageBox.ErrorQuery("Error", "Not connected", "Ok");
                return;
            }

            var topic = eventTopicText.Text;
            var json = eventJsonText.Text;
            
            if (topic.IsEmpty || json.IsEmpty)
            {
                MessageBox.ErrorQuery("Error", $"{(topic.IsEmpty ? "Topic" : "Json Event")} was empty", "Ok");
                return;
            }
            
            _exchange.Publish((string) json, (string) topic);

        };
        
        
        fView.Add(eventTopicLabel, eventTopicText, eventJsonLabel, eventJsonText, postButton);

        
        return fView;
    }
    

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _exchange?.Dispose();
    }

    private FrameView AddExchangeConnectControls()
    {
        var fView = new FrameView();
        fView.Width = Dim.Fill();
        fView.Height = 7;
        
        
        // Create input components and labels
        var hostLabel = new Label () { 
            Text = "Host:"
        };

        var hostText = new TextField ("localhost") {
            // Position text field adjacent to the label
            X = Pos.Right (hostLabel) + 1,

            // Fill remaining horizontal space
            Width = Dim.Fill (),
        };

        var exchangeLabel = new Label () {
            Text = "Exchange Name:",
            X = Pos.Left (hostLabel),
            Y = Pos.Bottom (hostLabel) + 1
        };

        var exchangeText = new TextField ("test_exchange") {
            // align with the text box above
            X = Pos.Right(exchangeLabel) + 1,
            Y = Pos.Top (exchangeLabel),
            Width = Dim.Fill (),
        };

        // Create login button
        var btnLogin = new Button () {
            Text = "Connect",
            Y = Pos.Bottom(exchangeLabel) + 1,
        };

        var btnDisconnect = new Button()
        {
            Text = "Disconnect",
            X = Pos.Right(btnLogin) + 1,
            Y = Pos.Bottom(exchangeLabel) + 1
        };

        btnDisconnect.Clicked += () =>
        {
            if (isConnected)
            {
                _exchange.Dispose();
                isConnected = false;
            }
            else
            {
                MessageBox.ErrorQuery("Error", "Not connected to any exchanges", "Ok");
            }
        };

        // When login button is clicked display a message popup
        btnLogin.Clicked += () =>
        {
            
            if (!isConnected && !hostText.Text.IsEmpty && !exchangeText.Text.IsEmpty)
            {
                try
                {
                    _exchange = new PublisherExchange(hostText.Text.ToString(), exchangeText.Text.ToString());
                    isConnected = true;
                    MessageBox.Query("Connected", "Connected to exchange", "Yay");
                }
                catch (Exception e)
                {
                    MessageBox.ErrorQuery("Connection Error", "Failed to connect to broker", "Ok");
                }
            }
            
            else
            {
                MessageBox.ErrorQuery("Error", "Host or Exchange was empty or already connected to something", "Ok");
            }
            
        };

        // Add the views to the Window
        fView.Add(hostLabel, hostText, exchangeLabel, exchangeText, btnLogin, btnDisconnect);
        
        return fView;
    }
}