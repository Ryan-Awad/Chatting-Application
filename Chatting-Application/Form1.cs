using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.Security;
using System.Security;

namespace Chatting_Application
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static IPAddress RHOST = IPAddress.Parse("127.0.0.1");
        static int RPORT = 0;
        static bool connected;
        static TcpListener listener;
        static TcpClient client;
        static Thread thread;
        static Thread thread2;
        static Thread thread3;
        // when listener sends 2 connect, connect see ok. rest is bad
        private void SendButton_Click(object sender, EventArgs e)
        {
            void Send()
            {
                NetworkStream nwStream = client.GetStream();

                char[] buffer = TextInput.Text.ToCharArray();
                byte[] convert = Encoding.UTF8.GetBytes(TextInput.Text);

                nwStream.Write(convert, 0, buffer.Length);
                Invoke(new Action(() =>
                {
                    OutputBox.Text += "[" + DateTime.Today + "] - " + TextInput.Text + "\n";
                }));
            }

            thread2 = new Thread(Send);
            thread2.Start();
        }

        private void ListenButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (PortBox.Text.ToString().Trim() == "0")
                {
                    MessageBox.Show("Please enter a valid IP Address or Port number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ListenButton.Enabled = true;
                    ConnectButton.Enabled = true;
                }
                else
                {
                    ListenButton.Enabled = false;
                    ConnectButton.Enabled = false;

                    RHOST = IPAddress.Parse(IpBox.Text);
                    RPORT = Convert.ToInt32(PortBox.Text);

                    listener = new TcpListener(RHOST, RPORT);

                    listener.Start();

                    OutputBox.Text = "Listening...";

                    void Connect()
                    {
                        client = listener.AcceptTcpClient();

                        Invoke(new Action(() =>
                        {
                            OutputBox.Text += "\n***[CONNECTED]***\n";
                            connected = true;
                            CloseButton.Enabled = true;
                        }));

                        try
                        {
                            while (true)
                            {
                                NetworkStream ns = client.GetStream();
                                byte[] buffer = new byte[client.ReceiveBufferSize];

                                int bytesRead = ns.Read(buffer, 0, client.ReceiveBufferSize);

                                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                                Invoke(new Action(() =>
                                {
                                    OutputBox.Text += "[" + DateTime.Now + "] - " + dataReceived + "\n";
                                }));
                            }
                        }
                        catch (IOException) // connection closed/lost
                        {
                            Invoke(new Action(() =>
                            {
                                listener.Stop();
                                thread.Abort();
                                OutputBox.Text += "\n**[CONNECTION CLOSED]**";
                                connected = false;
                                ListenButton.Enabled = true;
                                ConnectButton.Enabled = true;
                                CloseButton.Enabled = false;
                            }));
                        }
                    }


                    thread = new Thread(Connect);
                    thread.Start();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid IP Address or Port number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ListenButton.Enabled = true;
                ConnectButton.Enabled = true;
            }
            catch (SocketException)
            {
                MessageBox.Show("Please enter a valid IP Address or Port number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ListenButton.Enabled = true;
                ConnectButton.Enabled = true;
            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                ListenButton.Enabled = false;
                ConnectButton.Enabled = false;

                RHOST = IPAddress.Parse(IpBox.Text);
                RPORT = Convert.ToInt32(PortBox.Text);

                try
                {
                    OutputBox.Text = "Connecting...";
                    IPEndPoint serverEP = new IPEndPoint(RHOST, RPORT);
                    client = new TcpClient();
                    client.Connect(serverEP);
                    OutputBox.Text += "\n***[CONNECTED]***\n";
                    CloseButton.Enabled = true;
                    connected = true;

                    void Connect2() // SOLUTION TO CONNECTION CLOSING PROBLEM, MAKE THE RECEIVE LOOP IN A DIFFERENT THREAD THAN THE CONNECTION CLOSED EXEPCTION!
                    {
                        try
                        {
                            while (true)
                            {
                                NetworkStream ns = client.GetStream();
                                byte[] buffer = new byte[client.ReceiveBufferSize];

                                int bytesRead = ns.Read(buffer, 0, client.ReceiveBufferSize);

                                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                                Invoke(new Action(() =>
                                {
                                    OutputBox.Text += "[" + DateTime.Today + "] - " + dataReceived + "\n";
                                }));
                            }
                        }
                        catch (IOException) // connection closed/lost
                        {
                            Invoke(new Action(() =>
                            {
                                client.GetStream().Close();
                                client.Close();
                                thread3.Abort();
                                OutputBox.Text += "\n**[CONNECTION CLOSED]**";
                                connected = false;
                                ListenButton.Enabled = true;
                                ConnectButton.Enabled = true;
                                CloseButton.Enabled = false;
                            }));
                        }
                    }

                    thread3 = new Thread(Connect2);
                    thread3.Start();
                }
                catch (SocketException)
                {
                    OutputBox.Text += "\n**[NO CONNECTION]**";
                    connected = false;
                    ListenButton.Enabled = true;
                    ConnectButton.Enabled = true;
                    CloseButton.Enabled = false;
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid IP Address and Port number!", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                ListenButton.Enabled = true;
                ConnectButton.Enabled = true;
            }
            catch (SocketException)
            {
                MessageBox.Show("Please enter a valid IP Address or Port number!", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                ListenButton.Enabled = true;
                ConnectButton.Enabled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
            thread.Abort();
            thread2.Abort();
            thread3.Abort();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            client.Close();
            
            connected = false;
            ListenButton.Enabled = true;
            ConnectButton.Enabled = true;
            CloseButton.Enabled = false;
        }

        private void TextInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && connected == true)
            {
                void Send()
                {
                    NetworkStream nwStream = client.GetStream();

                    char[] buffer = TextInput.Text.ToCharArray();
                    byte[] convert = Encoding.UTF8.GetBytes(TextInput.Text);

                    nwStream.Write(convert, 0, buffer.Length);
                    Invoke(new Action(() =>
                    {
                        OutputBox.Text += "[" + DateTime.Now + "] - " + TextInput.Text + "\n";
                        TextInput.Text = null;
                    }));
                }

                thread2 = new Thread(Send);
                thread2.Start();
            }
        }
    }
}
