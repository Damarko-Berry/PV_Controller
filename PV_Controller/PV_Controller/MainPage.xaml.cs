
using System.Net.Sockets;
using System.Text;

namespace PV_Controller
{
	public partial class MainPage : ContentPage
	{
		ControllerRef controllerReference = null;
		TcpClient tcp = null;
		StreamWriter stream = null;
		public MainPage()
		{
			InitializeComponent();
			//SSDP.Clients = new();
			UpdateLayot(SSDP.Clients.Count>0);
		}

        private async void Search_Clicked(object sender, EventArgs e)
        {
            Search.Text = "Searching...";
            SemanticScreenReader.Announce(Search.Text);
            await SSDP.Scan();
            Search.Text = "Scan";
            SemanticScreenReader.Announce(Search.Text);


            UpdateLayot(SSDP.Clients.Count > 0);
        }
		void UpdateLayot(bool avail)
		{
            if (avail)
            {
                ClientList.ItemsSource = SSDP.Clients.Keys.ToArray();
            }
			if(avail & tcp == null)
			{
                ClientList.SelectedIndex = 0;
            }
			ControllerButtons.IsEnabled = avail;
		}

        private void NextChan_Clicked(object sender, EventArgs e)
        {
            if (controllerReference == null) return;

            SendCommand("NextChan");
            
        }

        private void PrevChan_Clicked(object sender, EventArgs e)
        {
            if (controllerReference == null) return;
            SendCommand("PrevChan");
            
        }

        private void VolumeUp_Clicked(object sender, EventArgs e)
        {
            if (controllerReference == null) return;

            SendCommand(VLCCommand.VolumeUp.ToString());
            
        }

        private void VulumeDown_Clicked(object sender, EventArgs e)
        {
            if (controllerReference == null) return;

            SendCommand(VLCCommand.VolumeDown.ToString());
            
        }

        private void Mute_Clicked(object sender, EventArgs e)
        {
            if (controllerReference == null) return;
            
            SendCommand(VLCCommand.Mute.ToString());
            
        }

        async void SendCommand(string command)
        {
            if (controllerReference == null) return;

            try
            {
                
                
                await stream.WriteLineAsync(command);
                
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error, show a message to the user)
                Console.WriteLine($"Error sending command: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            DisposeResources();
        }

        private void DisposeResources()
        {
            stream?.Dispose();
            tcp?.Close();
        }

        private void ClientList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(SSDP.Clients.Count == 0) return;
            DisposeResources();
            controllerReference = SSDP.Clients[ClientList.SelectedItem.ToString()];
            tcp = new TcpClient(controllerReference.Location, controllerReference.port);
            stream = new(tcp.GetStream()) { AutoFlush = true };
        }
    }

    enum VLCCommand
    {
        Play,
        VolumeDown,
        VolumeUp,
        Status,
        Mute,
        Quit,
    }
}