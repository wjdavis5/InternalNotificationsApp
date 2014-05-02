using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ININ.PSO.TimeSheetMonitor;
using Newtonsoft.Json;

namespace InternalNotification
{
    public partial class Form1 : Form
    {
        private static string CurrentUser { get { return string.Format("{0}@inin.com", Environment.UserName); } }
        private const string Iprojecturi = "https://iproject.inin.com/";
        private const string TitleText = "You Have Outstanding Timesheets!";
        private static readonly System.Timers.Timer _mainTimer = new System.Timers.Timer(1800000);
        private static readonly System.Timers.Timer _settingsTimer = new System.Timers.Timer(300000);
        private static readonly System.Timers.Timer _msgTimer = new System.Timers.Timer(1200000);
        private static List<string> _msgs = new List<string>();
        private static DateTime _lastUpdated;
        private static readonly object Locker = new object();
        private static readonly PopupNotifier p = new PopupNotifier(Iprojecturi);
        private const string ContentText =
            "Please goto IProject and update your timesheet.\r\nIf something is preventing this then please contact your manager or SO - HelpDesk.";

        private const string ManagerConentText = "You Have the following outstanding timesheets!! \r\n";


        public Form1()
        {
            try
            {
                InitializeComponent();
                _lastUpdated = DateTime.Now;
                _mainTimer.Elapsed += TimerOnElapsed;
                _msgTimer.Elapsed += MsgTimerOnElapsed;
                _settingsTimer.Elapsed += SettingsTimerOnElapsed;
                
                _msgTimer.Enabled = true;
                _settingsTimer.Enabled = true;
                _mainTimer.Enabled = true;

                var t = Task.Factory.StartNew(() => TimerOnElapsed(null, null));
            }
            catch(Exception)
            {}
        }

        private static void SettingsTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                _settingsTimer.Enabled = false;
                
                var settings = GetSettings();
                ProcessSettings(settings);
            }
            catch (Exception)
            {}

            finally
            {
                _settingsTimer.Enabled = true;
            }

        }

        private void MsgTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                _msgTimer.Enabled = false;
                lock(Locker)
                    _msgs.AddRange(GetMessages());
                if (_msgs.Any())
                    ExecuteSecure(ShowMessages);
            }
            catch (Exception)
            {
            }
            finally
            {
                _msgTimer.Enabled = true;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                _mainTimer.Enabled = false;
                if (HasOutstandingTimesheet(GetTimeSheets()))
                    ExecuteSecure(ShowToast);
            }
            catch (Exception)
            {
            }
            finally
            {
                _mainTimer.Enabled = true;
            }
        }

        private void ExecuteSecure(Action a)
        {
            
                if (InvokeRequired)
                        BeginInvoke(a);
                else
                    a();
        }

        private static bool HasOutstandingTimesheet(IEnumerable<HumanTimesheet> list)
        {
            return list.Any(humanTimesheet => humanTimesheet.EmailAddress.ToLower() == CurrentUser.ToLower());
        }

        private static IEnumerable<string> GetMessages()
        {
            try
            {
                var wC = new WebClient();
                var result = wC.DownloadString("http://psodata1:8091/AnnualReviewService/json/GetBroadcastMessage?user="+ CurrentUser);

                return JsonConvert.DeserializeObject<List<string>>(result);
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private static Dictionary<string,string> GetSettings()
        {
            try
            {
                var wC = new WebClient();
                var result = wC.DownloadString("http://psodata1:8091/AnnualReviewService/json/GetSettings?user=" + CurrentUser);

                return JsonConvert.DeserializeObject<Dictionary<string,string>>(result);
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private static void ProcessSettings(Dictionary<string, string> settings)
        {
            try
            {
                
            }
            catch(Exception)
            {}
        }

        private static void ShowToast(string titleText, string contentText)
        {

            try
            {
                p.TitleText = titleText;
                p.BodyColor = Color.DarkRed;
                p.ContentColor = Color.WhiteSmoke;
                p.ContentText = contentText;
                p.Popup();
            }
            catch(Exception){}
        }

        private static void ShowToast()
        {
            try
            {
                p.TitleText = TitleText;
                p.BodyColor = Color.DarkRed;
                p.ContentColor = Color.WhiteSmoke;
                p.ContentText = ContentText;
                
                p.Popup();
            }
            catch (Exception)
            {
            }
        }

        private static void ShowMessages()
        {
            lock (Locker)
            {
                p.TitleText = "Broadcast Message!!";
                p.BodyColor = Color.Green;
                p.ContentColor = Color.WhiteSmoke;
                p.ContentText = string.Join("\r\n", _msgs);
                p.Popup();
                _msgs.Clear();
            }
            
        }

        private static IEnumerable<HumanTimesheet> GetTimeSheets()
        {
            try
            {
                var wC = new WebClient();
                //we append the currentuser to the string to allow for tracking of requests later.
                var result = wC.DownloadString("http://psodata1:8091/AnnualReviewService/json/GetTimeSheets?user="+ CurrentUser);

                return JsonConvert.DeserializeObject<List<HumanTimesheet>>(result);
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
