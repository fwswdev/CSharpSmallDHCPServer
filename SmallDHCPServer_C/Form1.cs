#region Copyright Information
/*
 * (C)  2005-2007, Marcello Cauchi Savona
 *
 * For terms and usage, please see the LICENSE file
 * provided alongwith or contact marcello_c@hotmail.com
 * 
 * http://www.cheekyneedle.com
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace SmallDHCPServer_C
{
    

    public partial class Form1 : Form
    {
        private clsDHCP cDhcp;
        private Button tt= new Button();

        private delegate void Adff();
      
        delegate void SimpleDelegate();
        public string startIp, MacMask, AdapterIP;
        public bool NetCardAlive = false;
        private Thread dHCPThread = null;
      


        public Form1()
        {
            InitializeComponent();
            //ini the text strings
            label3.Text = "Enter the Mac Mask: \n(leave empty to allow all)";
            //fill up the adapter list box with info
            //get the default settings from registry
            textBox1.Text = (string)Registry.LocalMachine.GetValue("IpAdd", "10.192.10.22");
            textBox2.Text = (string)Registry.LocalMachine.GetValue("MacMask", "00"); //add more data to this to block 
            button4.Text = "Start DHCP Server";
           
           
           // AddListLine AddlistLineDelegate = new AddListLine(AddLineToList);
         }


        public static bool CheckAlive(string IpAdd)
        {
            Ping pingSender = new Ping();
            IPAddress address;
            PingReply reply;


            try
            {
                 address = IPAddress.Parse(IpAdd);//IPAddress.Loopback;
                 reply = pingSender.Send(address,100);
                 if (reply.Status == IPStatus.Success)
                 {
                     Console.WriteLine("Address: {0}", reply.Address.ToString());
                     Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                     Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                     Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                     Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                     return true;
                 }
                 else
                 {
                     Console.WriteLine(reply.Status);
                     return false;
                 }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            finally
            {
                if (pingSender != null)  pingSender.Dispose();
                pingSender = null;
                address = null;
                reply = null;
            }
         }


        private static uint IPAddressToLongBackwards(string IPAddr)
        {
            System.Net.IPAddress oIP = System.Net.IPAddress.Parse(IPAddr);
            byte[] byteIP = oIP.GetAddressBytes();


            uint ip = (uint)byteIP[0] << 24;
            ip += (uint)byteIP[1] << 16;
            ip += (uint)byteIP[2] << 8;
            ip += (uint)byteIP[3];

            return ip;
        }

        private  string GetIPAdd()
        {
            IPAddress ipadd;
            byte[] yy;
            UInt32 iit;
            try
            {
                iit = IPAddressToLongBackwards(startIp);
                iit -= 1;
                do
                {
                    iit += 1;
                    yy = new byte[4];
                    yy[3] = (byte)(iit);
                    yy[2] = (byte)(iit >> 8);
                    yy[1] = (byte)(iit >> 16);
                    yy[0] = (byte)(iit >> 24);
                    ipadd = new IPAddress(yy);
                   // yy = IPAddress.HostToNetworkOrder(ii);
                }
                while (CheckAlive(ipadd.ToString()) == true);
                //reaching here means that the ip is free

                return ipadd.ToString();
            }
            catch 
            {
                return null;
            }
        }

        public void cDhcp_Announced(cDHCPStruct d_DHCP, string MacId)
        {
            string str = string.Empty;

            if (MacId.StartsWith(MacMask) == true)
            {
                //options should be filled with valid data
                d_DHCP.dData.IPAddr = GetIPAdd();
                d_DHCP.dData.SubMask = "255.255.0.0";
                d_DHCP.dData.LeaseTime = 2000;
                d_DHCP.dData.ServerName = "Small DHCP Server";
                d_DHCP.dData.MyIP = AdapterIP;
                d_DHCP.dData.RouterIP = "0.0.0.0";
                d_DHCP.dData.LogServerIP = "0.0.0.0";
                d_DHCP.dData.DomainIP = "0.0.0.0";
                str = "IP requested for Mac: " + MacId;
               
            }
            else
            {
                str = "Mac: " + MacId + " is not part of the mask!";
            }
            cDhcp.SendDHCPMessage(DHCPMsgType.DHCPOFFER, d_DHCP);
            SimpleDelegate lst = delegate
            {
                listBox1.Items.Add(str);
            };
            Invoke(lst);
        //Application.DoEvents()
        }

        public void cDhcp_Request(cDHCPStruct d_DHCP, string MacId)
        {
            string str = string.Empty;
            if (MacId.StartsWith(MacMask) == true)
            {
                //announced so then send the offer
                d_DHCP.dData.IPAddr = GetIPAdd(); 
                d_DHCP.dData.SubMask = "255.255.0.0";
                d_DHCP.dData.LeaseTime = 2000; 
                d_DHCP.dData.ServerName = "tiny DHCP Server";
                d_DHCP.dData.MyIP = AdapterIP;
                d_DHCP.dData.RouterIP = "0.0.0.0";
                d_DHCP.dData.LogServerIP = "0.0.0.0";
                d_DHCP.dData.DomainIP = "0.0.0.0";
                cDhcp.SendDHCPMessage(DHCPMsgType.DHCPACK, d_DHCP);
                str = "IP " + d_DHCP.dData.IPAddr + " Assigned to Mac: " + MacId;
            }
            else 
            {
                str = "Mac: " + MacId + " is not part of the mask!";
            }
            SimpleDelegate lst = delegate
            {
                listBox1.Items.Add(str);
            };
            
            Invoke(lst);
        }

        public void oou()
        {
            Console.WriteLine("LL");
        }


         private void button4_Click(object sender, EventArgs e)
        {
            switch (button4.Text)
            {
                case "Start DHCP Server":

                    //save the values to regisrtyy
                    Registry.LocalMachine.SetValue("IPAdd", textBox1.Text);
                                       Registry.LocalMachine.SetValue("MacMask", textBox2.Text);


                    AdapterIP = "0.0.0.0"; //allow data from all network cards
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    button4.Text = "Stop DHCP Server";
                    cDhcp = new clsDHCP(AdapterIP);
                    cDhcp.Announced += new clsDHCP.AnnouncedEventHandler(cDhcp_Announced);
                    cDhcp.Request += new clsDHCP.RequestEventHandler(cDhcp_Request);
                    startIp = textBox1.Text;
                    MacMask = textBox2.Text;
                    dHCPThread = new Thread(cDhcp.StartDHCPServer);
                    dHCPThread.Start();
                    break;
                default:
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    button4.Text = "Start DHCP Server";
                    cDhcp.Dispose();

                    break;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
          
                   Application.DoEvents();
            button4.PerformClick();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cDhcp.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }                     
   }
}