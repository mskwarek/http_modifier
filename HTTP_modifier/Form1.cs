using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fiddler;
using System.Threading;

namespace HTTP_modifier
{
    public partial class Form1 : Form
    {
        List<Fiddler.Session> oAllSessions = new List<Fiddler.Session>();
        private Logs logs;
        private string toChange = null;
        private string changeFor = null;


        static Proxy oSecureEndpoint;
        static string sSecureEndpointHostname = "localhost";
        static int iSecureEndpointPort = 7777;

        // <-- Personalize for your Application, 64 chars or fewer

        public Form1()
        {
            InitializeComponent();
            logs = new Logs(this.listView1);
            //this.appThread = new Thread(new ThreadStart(InitSniffing));
            //this.appThread.Start();
        }

        private void InitSniffing()
        {
            Fiddler.FiddlerApplication.SetAppDisplayName("FiddlerCoreDemoApp");
            //while (true)
           // {
                List<Fiddler.Session> oAllSessions = new List<Fiddler.Session>();

                // <-- Personalize for your Application, 64 chars or fewer
                Fiddler.FiddlerApplication.SetAppDisplayName("FiddlerCoreDemoApp");

                #region AttachEventListeners
                //
                // It is important to understand that FiddlerCore calls event handlers on session-handling
                // background threads.  If you need to properly synchronize to the UI-thread (say, because
                // you're adding the sessions to a list view) you must call .Invoke on a delegate on the 
                // window handle.
                // 
                // If you are writing to a non-threadsafe data structure (e.g. List<t>) you must
                // use a Monitor or other mechanism to ensure safety.
                //

                // Simply echo notifications to the console.  Because Fiddler.CONFIG.QuietMode=true 
                // by default, we must handle notifying the user ourselves.
                Fiddler.FiddlerApplication.OnNotification += delegate(object sender, NotificationEventArgs oNEA) { Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); };
                Fiddler.FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); };

                Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
                {
                    // Console.WriteLine("Before request for:\t" + oS.fullUrl);
                    // In order to enable response tampering, buffering mode MUST
                    // be enabled; this allows FiddlerCore to permit modification of
                    // the response in the BeforeResponse handler rather than streaming
                    // the response to the client as the response comes in.
                    oS.bBufferResponse = false;
                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    Monitor.Exit(oAllSessions);
                    oS["X-AutoAuth"] = "(default)";

                    /* If the request is going to our secure endpoint, we'll echo back the response.
                
                    Note: This BeforeRequest is getting called for both our main proxy tunnel AND our secure endpoint, 
                    so we have to look at which Fiddler port the client connected to (pipeClient.LocalPort) to determine whether this request 
                    was sent to secure endpoint, or was merely sent to the main proxy tunnel (e.g. a CONNECT) in order to *reach* the secure endpoint.

                    As a result of this, if you run the demo and visit https://localhost:7777 in your browser, you'll see

                    Session list contains...
                 
                        1 CONNECT http://localhost:7777
                        200                                         <-- CONNECT tunnel sent to the main proxy tunnel, port 8877

                        2 GET https://localhost:7777/
                        200 text/html                               <-- GET request decrypted on the main proxy tunnel, port 8877

                        3 GET https://localhost:7777/               
                        200 text/html                               <-- GET request received by the secure endpoint, port 7777
                    */
                    //if (oS.port == 80 || oS.clientPort == 80 || oS.RequestMethod == "POST")
                    //{
                        logs.addLog("Method: " + oS.RequestMethod + " URL: " + oS.url + " " + "\n", true, 0, true);
                        //logs.addLog(oS.GetResponseBodyAsString(), true, 0, true);
                    //}
                    
                    
                     if (toChange != null && changeFor != null && oS.url.Contains(toChange))
                    {
                        logs.addLog(changeFor, true, 2, true);
                        oS.utilCreateResponseAndBypassServer();
                        oS.oResponse.headers.SetStatus(307, "Redirect");
                        oS.oResponse["Cache-Control"] = "nocache";
                        oS.oResponse["Location"] = "http://www."+changeFor;
                        oS.utilSetResponseBody("<html><body>sending request elsewhere</body></html>");
                        return;
                    }

                    
                    else if ((oS.oRequest.pipeClient.LocalPort == iSecureEndpointPort) && (oS.hostname == sSecureEndpointHostname))
                    {
                        oS.utilCreateResponseAndBypassServer();
                        oS.oResponse.headers.SetStatus(200, "Ok");
                        oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                        oS.oResponse["Cache-Control"] = "private, max-age=0";
                        oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
                    }
                };

                /*
                    // The following event allows you to examine every response buffer read by Fiddler. Note that this isn't useful for the vast majority of
                    // applications because the raw buffer is nearly useless; it's not decompressed, it includes both headers and body bytes, etc.
                    //
                    // This event is only useful for a handful of applications which need access to a raw, unprocessed byte-stream
                    Fiddler.FiddlerApplication.OnReadResponseBuffer += new EventHandler<RawReadEventArgs>(FiddlerApplication_OnReadResponseBuffer);
                */

                /*
                Fiddler.FiddlerApplication.BeforeResponse += delegate(Fiddler.Session oS) {
                    // Console.WriteLine("{0}:HTTP {1} for {2}", oS.id, oS.responseCode, oS.fullUrl);
                
                    // Uncomment the following two statements to decompress/unchunk the
                    // HTTP response and subsequently modify any HTTP responses to replace 
                    // instances of the word "Microsoft" with "Bayden". You MUST also
                    // set bBufferResponse = true inside the beforeREQUEST method above.
                    //
                    //oS.utilDecodeResponse(); oS.utilReplaceInResponse("Microsoft", "Bayden");
                };*/

                Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
                {
                    //Console.WriteLine("Finished session:\t" + oS.fullUrl); 
                    //Console.Title = ("Session list contains: " + oAllSessions.Count.ToString() + " sessions");
                };

                // Tell the system console to handle CTRL+C by calling our method that
                // gracefully shuts down the FiddlerCore.
                //
                // Note, this doesn't handle the case where the user closes the window with the close button.
                // See http://geekswithblogs.net/mrnat/archive/2004/09/23/11594.aspx for info on that...
                //
                //Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
                #endregion AttachEventListeners


                string sSAZInfo = "NoSAZ";
#if SAZ_SUPPORT
            sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;

            // You can load Transcoders from any different assembly if you'd like, using the ImportTranscoders(string AssemblyPath) 
            // overload.
            //
            //if (!FiddlerApplication.oTranscoders.ImportTranscoders(Assembly.GetExecutingAssembly()))
            //{
            //    Console.WriteLine("This assembly was not compiled with a SAZ-exporter");
            //}

            DNZSAZProvider.fnObtainPwd = () =>
            {
                Console.WriteLine("Enter the password (or just hit Enter to cancel):");
                string sResult = Console.ReadLine();
                Console.WriteLine();
                return sResult;
            };

            FiddlerApplication.oSAZProvider = new DNZSAZProvider();
#endif

                Console.WriteLine(String.Format("Starting {0} ({1})...", Fiddler.FiddlerApplication.GetVersionString(), sSAZInfo));

                // For the purposes of this demo, we'll forbid connections to HTTPS 
                // sites that use invalid certificates. Change this from the default only
                // if you know EXACTLY what that implies.
                Fiddler.CONFIG.IgnoreServerCertErrors = false;

                // ... but you can allow a specific (even invalid) certificate by implementing and assigning a callback...
                // FiddlerApplication.OnValidateServerCertificate += new System.EventHandler<ValidateServerCertificateEventArgs>(CheckCert);

                FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

                // For forward-compatibility with updated FiddlerCore libraries, it is strongly recommended that you 
                // start with the DEFAULT options and manually disable specific unwanted options.
                FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;

                // E.g. If you want to add a flag, start with the .Default and "OR" the new flag on:
                // oFCSF = (oFCSF | FiddlerCoreStartupFlags.CaptureFTP);

                // ... or if you don't want a flag in the defaults, "and not" it out:
                // Uncomment the next line if you don't want FiddlerCore to act as the system proxy
                // oFCSF = (oFCSF & ~FiddlerCoreStartupFlags.RegisterAsSystemProxy);

                // *******************************
                // Important HTTPS Decryption Info
                // *******************************
                // When FiddlerCoreStartupFlags.DecryptSSL is enabled, you must include either
                //
                //     MakeCert.exe
                //
                // *or*
                //
                //     CertMaker.dll
                //     BCMakeCert.dll
                //
                // ... in the folder where your executable and FiddlerCore.dll live. These files
                // are needed to generate the self-signed certificates used to man-in-the-middle
                // secure traffic. MakeCert.exe uses Windows APIs to generate certificates which
                // are stored in the user's \Personal\ Certificates store. These certificates are
                // NOT compatible with iOS devices which require specific fields in the certificate
                // which are not set by MakeCert.exe. 
                //
                // In contrast, CertMaker.dll uses the BouncyCastle C# library (BCMakeCert.dll) to
                // generate new certificates from scratch. These certificates are stored in memory
                // only, and are compatible with iOS devices.

                // Uncomment the next line if you don't want to decrypt SSL traffic.
                // oFCSF = (oFCSF & ~FiddlerCoreStartupFlags.DecryptSSL);

                // NOTE: In the next line, you can pass 0 for the port (instead of 8877) to have FiddlerCore auto-select an available port
                int iPort = 8877;
                Fiddler.FiddlerApplication.Startup(iPort, oFCSF);

                FiddlerApplication.Log.LogFormat("Created endpoint listening on port {0}", iPort);

                FiddlerApplication.Log.LogFormat("Starting with settings: [{0}]", oFCSF);
                FiddlerApplication.Log.LogFormat("Gateway: {0}", CONFIG.UpstreamGateway.ToString());

                Console.WriteLine("Hit CTRL+C to end session.");




                // We'll also create a HTTPS listener, useful for when FiddlerCore is masquerading as a HTTPS server
                // instead of acting as a normal CERN-style proxy server.
                oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(iSecureEndpointPort, true, sSecureEndpointHostname);



                if (null != oSecureEndpoint)
                {
                    FiddlerApplication.Log.LogFormat("Created secure endpoint listening on port {0}, using a HTTPS certificate for '{1}'", iSecureEndpointPort, sSecureEndpointHostname);
                }
          //  }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            toChange = textBox1.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitSniffing();
            this.button1.Enabled = false;
            this.textBox1.Enabled = true;
            this.textBox2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
            Fiddler.FiddlerApplication.Shutdown();
            Thread.Sleep(500);
            this.button1.Enabled = true;
            this.textBox1.Enabled = false;
            this.textBox2.Enabled = false;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            changeFor = textBox2.Text;
        }
    }
}
