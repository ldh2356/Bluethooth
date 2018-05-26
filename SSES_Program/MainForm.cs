﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DeviceAgents;
using EnergySolutions;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Management;
using Microsoft.Win32;

namespace SSES_Program
{
    /// <summary>
    /// 메인폼
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// 블루투스 핸들 클래스
        /// </summary>
        Bt32FeetDevice bt32FeetDevice = new Bt32FeetDevice();

        /// <summary>
        /// 계산 라이브러리
        /// </summary>
        CalcReduction calcReduction = new CalcReduction();

        /// <summary>
        /// 유저의 마우스 또는 키보드 입력이 들어왔는지 체크하는 변수
        /// </summary>
        public bool isUserInput { get; set; } = true;

        public string DevAddrs;
        public int userRssi = -65; // 사용자가 정한 RSSI값
        public double ratedOutput_device = 160.0;
        public double power_reduction = 0.0;
        public double electricCost_reduction = 0.0;
        public double co2_reduction = 0.0;
        public double tree_reduction = 0.0;
        public int TotreductionSecond = 0;
        public static string userPw = "0000";
        public List<int> RcvBuffer = new List<int>();
        public const int RcvMaxCount = 1;
        public int rcvRssi = default(int); // 시리얼 포트에서 받아 온 이전 RSSI값
        public bool screensaverStatus;
        public bool screensaverPasswordflag;
        FormScreenSaver screenSaver;
        FormScreenSaver2 screenSaver2;

        Thread inputThread = null;


        SendPCEnergy sPCEnergy = new SendPCEnergy();
       
        public string _macAddress = string.Empty;
        public string _manufacturer = string.Empty;
        public string _modelName = string.Empty;
        public string _CPU = string.Empty;
        public string _memory = string.Empty;
        public string _graphicsCard = string.Empty;
        private System.Timers.Timer timer;

        public static int threadTimerCount = 0;


        public string _uptime = string.Empty;
        public string _savingTime = string.Empty;

        GetPCEnergy gPCEnergy = new GetPCEnergy();

        PCInfo pcInfo = new PCInfo();

        public Log log = new Log();

        private Hook.KeyboardHook keyHook = new Hook.KeyboardHook();
        private Hook.MouseHook mouseHook = new Hook.MouseHook();
        private delegate void UIInvokerDelegate(String msg);
        private UIInvokerDelegate UIInvoker;

        /// <summary>
        /// 컨피그 파일 이름 세팅
        /// </summary>
        #region filename
        public static string AppConfigFileName
        {
            get
            {
                string drivepath = Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\HansCreative\nnv\SSES_Program";
                string fileName = @"\SSES_config.xml";
                return drivepath + fileName;
            }
        }
        #endregion



        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        Keys lastKeyPressed = Keys.None;



        private int WM_LBUTTONUP = 0x0202; //left mouse up
        private int WM_MBUTTONUP = 0x0208; //middle mouse up
        private int WM_RBUTTONUP = 0x0205; //right mouse up
        private int keyOrMouseInputDelayMin = 3;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((msg.Msg == WM_KEYDOWN) || (msg.Msg == WM_SYSKEYDOWN))
            {
                lastKeyPressed = keyData;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

 
        /// <summary>
        /// 키보드 마우스 입력 타이머 이벤트
        /// </summary>
        private void DoInputTimer()
        {
            try
            {
                if (timer == null)
                    timer = new System.Timers.Timer();

                timer.Elapsed += DoInDoInputTimerAsyncputTimer;
                timer.Interval = 1000;
                timer.Start();
            }
            catch (Exception)
            {
                throw;
            }
        }



        /// <summary>
        /// 키보드 마우스 입력시 3 초만큼 딜레이를 준다
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoInDoInputTimerAsyncputTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                threadTimerCount++;                

                if (threadTimerCount > keyOrMouseInputDelayMin)
                {
                    timer.Stop();
                    isUserInput = false;
                }
                else
                {
                    isUserInput = true;
                }    
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        /// <summary>
        /// 키보드 또는 마우스 입력이 있는경우 정해진 시간동안은 스크린세이버가 들어오는것을 방지한다
        /// </summary>
        public void donUseSceenSaveWhenUserInput()
        {
            timer.Stop();
            threadTimerCount = 0;
            bt32FeetDevice.LockCount = 0;
            timer.Start();
        }


        /// <summary>
        /// Win32 키보드 후킹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void keyHook_MessageHooked(object sender, Hook.KeyboardHookEventArgs e)
        {
            donUseSceenSaveWhenUserInput();
            BeginInvoke(this.UIInvoker, e.ToString());
        }


        /// <summary>
        /// Win32 마우스 후킹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mouseHook_MessageHooked(object sender, Hook.MouseHookEventArgs e)
        {
            donUseSceenSaveWhenUserInput();
            BeginInvoke(this.UIInvoker, e.ToString());
        }



        public void DiplayMessage(String msg)
        {
            Console.WriteLine(msg);
            // 이 안의 내용 변경하면 됩니다.
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if(e.Mode == PowerModes.Suspend)
            {
                bt32FeetDevice.Stop();
            }

            if(e.Mode == PowerModes.Resume)
            {
                Console.WriteLine("qqqqqqqqqqqqq");
                bt32FeetDevice.Start();
            }
        }


        public MainForm()
        {
            try
            {
                screensaverStatus = false;
                screensaverPasswordflag = false;

                log.write("SSES 실행 v1.2");
                DoInputTimer();

                //자동 업데이트 추가 
                if (UpdateChecker.NeedUpdate(this))
                {
                    if (MessageBox.Show(SsesRes.ClientUpdatedCheck, SsesRes.StringConfirm, MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
                    {
                        UpdateChecker.RunClientUpdater();
                        this.DialogResult = DialogResult.Cancel;
                        return;
                    }
                }

                sPCEnergy.hardwardInfo = new Dictionary<string, string>();
                getHardwardInfo();

                calcReduction._OperationStartTime = DateTime.Now;
                calcReduction.IsSend = "true";
                this.sendPCEnergy("1");

                AppConfig.Instance.FileName = AppConfigFileName;
                AppConfig.Instance.LoadFromFile();
                InitializeComponent();

                bt32FeetDevice._model = AppConfig.Instance.Model;

                DevAddrs = AppConfig.Instance.DeviceAddress;
                DisplayAddToText(DevAddrs);
                Console.WriteLine("Start Device Address is {0}", DevAddrs);
    
                if (DevAddrs != "00:00:00:00:00:00")
                {
                    bt32FeetDevice.GetBtAddr(DevAddrs);
                    bt32FeetDevice.OnData += On32FeetData;
                    bt32FeetDevice.Start();
                }
                else
                {
                    MessageBox.Show(SsesRes.execution_caution_msg, SsesRes.execution_caution);
                }

                userRssi = AppConfig.Instance.TrackBar;
                ratedOutput_device = AppConfig.Instance.PcPower;

              

                TotreductionSecond = AppConfig.Instance.TotalTime;
                userPw = AppConfig.Instance.UserPassword;
                trackBar.Value = userRssi;

                if (AppConfig.Instance.SleepMode == 1)
                {
                    this.sleepModeUserControl1.RbPcMode.Checked = true;
                    this.sleepModeUserControl1.RbMonitorMode.Checked = false;
                }
                else
                {
                    this.sleepModeUserControl1.RbMonitorMode.Checked = true;
                    this.sleepModeUserControl1.RbPcMode.Checked = false;
                }



                if (AppConfig.Instance.Model == 1)
                {
                    this.deviceUserControl1.RadioButton2.Checked = true;
                    this.deviceUserControl1.RadioButton1.Checked = false;
                }
                else
                {
                    this.deviceUserControl1.RadioButton1.Checked = true;
                    this.deviceUserControl1.RadioButton2.Checked = false;
                }
                //_CalcReduc.DevicePerKwh = 160.0; // 원래 있던거
                calcReduction.DevicePerKwh = AppConfig.Instance.PcPower; // 추가한거
                calcReduction.WonPerKwh = AppConfig.Instance.ElecRate; // 추가한거
                //_CalcReduc.Calculate(); // 추가한거
                calcReduction.OnSaveChanged += (sender) =>
                {
                /*textBlock_power.Text = String.Format("{0,10:N3}", sender.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", sender.UsedCost).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", sender.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", sender.Tree).ToString();*/
                //label_totTime1.Text = ("총 보안시간은 " + String.Format("{0:00}일 {1:00}:{2:00}:{3:00}", sender.UsedSec.Days, Math.Floor(sender.UsedSec.TotalHours), sender.UsedSec.Minutes, sender.UsedSec.Seconds).ToString() + "입니다. ");// + tempMinutes.ToString() + "분 " + tempSeconds.ToString() + "초 입니다." + " \n");
                //label_dispTotTime.Text = "SSES 솔루션을 통한 PC의 " + label_totTime1.Text;
                label_dispTotTime.Text = SsesRes.dispTotTime
                        + String.Format("{0:00}" + SsesRes.day + " {1:00}:{2:00}:{3:00}", sender.UsedSec.Days, Math.Floor(sender.UsedSec.TotalHours), sender.UsedSec.Minutes, sender.UsedSec.Seconds).ToString() + "입니다.";
                };
                calcReduction.OnSaveChanged(calcReduction);
                Console.WriteLine("_main");
            }
            catch(Exception ex)
            {
                Console.WriteLine("main :" + ex.ToString());
            }
        }

        #region "API 연동"


        /// <summary>
        /// 인터넷 연결 여부를 체크한다. 
        /// </summary>
        /// <param name="Description"></param>
        /// <param name="ReservedValue"></param>
        /// <returns></returns>
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }


        /// <summary>
        /// 사용 안함 - DLL 자동 업데이트 사용 예정 
        /// </summary>
        public void getAppVer()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string json = wc.DownloadString("http://dev.i-mobilepark.com/getAppVer?ver=1.0.0"); //API 사이트에서 json 받아옴
                    json = "[" + json + "]";
                    JArray jarr = JArray.Parse(json); //json 객체로
                    foreach (JObject jobj in jarr)
                    {
                        MessageBox.Show(jobj["ver"].ToString() + " ,  " + jobj["verYn"].ToString() + ",  " + jobj["updateUrl"].ToString()); //플러그인명,버전,url 출력
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("버전 체크");
            }
        }


        /// <summary>
        /// PC 에너지 정보 전송
        /// </summary>
        /// <param name="eventType"></param>
        public void sendPCEnergy(string eventType)
        {
            string savingTime = "0";
            string uptime = "0";
            try
            {
                savingTime = Convert.ToString(this.calcReduction.UsedSec.TotalSeconds * 1000);
            }
            catch
            {
                savingTime = "0";
            }

            try
            {
                uptime = Convert.ToString(this.calcReduction.UsedOperation.TotalSeconds * 1000);
            }
            catch
            {
                uptime = "0";
            }
            this.sPCEnergy.uptime = uptime;
            this.sPCEnergy.uptime = TimeSpan.FromMilliseconds(DateTime.Now.Millisecond).ToString();
            this.sPCEnergy.savingTime = calcReduction.UsedSec.ToString();
        }

        /// <summary>
        /// PC 에너지 절감량 조회
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        public void getPCEnergy(string fromDate, string toDate)
        {
            if (checkBox_LangToggle.Text.Equals("English"))
            {
                textBlock_power.Text = String.Format("{0,10:N3}", calcReduction.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", calcReduction.UsedCost).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", calcReduction.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", calcReduction.Tree).ToString();
            }
            else
            {
                textBlock_power.Text = String.Format("{0,10:N3}", calcReduction.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", (calcReduction.UsedCost/1200)).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", calcReduction.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", calcReduction.Tree).ToString();
            }
        }

        /// <summary>
        /// 누적 upTime, savingTime 조회 
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        public void getUpTimeSavingTime()
        {
            DateTime now = DateTime.Now;
            var epoch = new DateTime(1970, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            string uptime = Convert.ToString(Convert.ToInt64((now - epoch).TotalSeconds) * 1000);
            double timestamp = Convert.ToDouble(uptime);
            this.sPCEnergy.uptime = uptime;
            this.sPCEnergy.uptime = TimeSpan.FromMilliseconds(DateTime.Now.Millisecond).ToString();
            this.sPCEnergy.savingTime = calcReduction.UsedSec.ToString();
        }


        /// <summary>
        /// 그래픽 카드
        /// </summary>
        void GetVGA() // 그래픽 카드
        {
            ManagementObjectSearcher MOS = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            String str, des, vga = String.Empty;
            int locs, locl;
            //Console.WriteLine("#그래픽 카드");

            foreach (ManagementObject VGA in MOS.Get())
            {
                str = VGA.GetText(TextFormat.Mof); // 전체 내용

                //Console.WriteLine(str); // 전체 내용 출력
                locs = str.IndexOf("\tDescription"); // 모델명 위치 번호
                //Console.WriteLine(locs); // 모델명 위치 번호 출력
                des = str.Substring(locs); // descripton부터의 내용
                locs = des.IndexOf("\"");
                locl = des.IndexOf(";");
                vga += des.Substring(locs + 1, locl - locs - 2) + "\n"; // 모델명 얻기
                this._graphicsCard = vga;
            }
        }

        void GetProcessor() // CPU
        {
            ManagementObjectSearcher MOS = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

            String str, nam, prc = string.Empty;
            int locs, locl;
            //Console.WriteLine("#CPU");

            foreach (ManagementObject PRC in MOS.Get())
            {
                str = PRC.GetText(TextFormat.Mof); // 전체 내용

                //Console.WriteLine(str); // 전체 내용 출력
                locs = str.IndexOf("\tName"); // 모델명 위치 번호
                //Console.WriteLine(locs); // 모델명 위치 번호 출력
                nam = str.Substring(locs); // descripton부터의 내용
                locs = nam.IndexOf("\"");
                locl = nam.IndexOf(";");
                prc = nam.Substring(locs + 1, locl - locs - 2); // 모델명 얻기
                Console.WriteLine("-" + prc);

                this._CPU = prc;
            }

        }

        /// <summary>
        /// CPU 정보 
        /// </summary>
        public void GetCPU()
        {
            string cpuInfo = String.Empty;
            string temp = String.Empty;
            ManagementClass mc = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (cpuInfo == String.Empty)
                {// only return cpuInfo from first CPU
                    cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
                }
            }
            this._CPU = cpuInfo;
            //Console.WriteLine("-" + cpuInfo);
            //return cpuInfo;
        }


        /// <summary>
        /// 맥 어드레스 정보
        /// </summary>
        public void GetMACAddress()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string MACAddress = String.Empty;
            foreach (ManagementObject mo in moc)
            {
                if (MACAddress == String.Empty)  // only return MAC Address from first card
                {
                    if ((bool)mo["IPEnabled"] == true) MACAddress = mo["MacAddress"].ToString();
                }
                mo.Dispose();
            }

            //MACAddress = MACAddress.Replace(":", "");
            this._macAddress = MACAddress;

            //Console.WriteLine("-" + MACAddress);
        }

        /// <summary>
        /// 메모리정보
        /// </summary>
        public void GetMemory()
        {
            ManagementClass cls = new ManagementClass("Win32_OperatingSystem");
            // ManagementClass cls = new ManagementClass("Win32_LogicalMemoryConfiguration");
            ManagementObjectCollection instances = cls.GetInstances();
            // 사실상 싱글톤 객체이므로 이 코드는 1회만 수행된다.
            foreach (ManagementObject info in instances)
            {
                Console.WriteLine("Memory Information ================================");
                Console.WriteLine("Total Physical Memory : {0:#,###} KB", info["TotalVisibleMemorySize"]);

                this._memory = info["TotalVisibleMemorySize"].ToString();
            }
        }
        public void Manufacturer()
        {
            // create management class object
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
            //collection to store all management objects
            ManagementObjectCollection moc = mc.GetInstances();
            if (moc.Count != 0)
            {
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    this._manufacturer = mo["Manufacturer"].ToString();
                    // display general system information
                    Console.WriteLine("\nMachine Make: {0}",
                                      mo["Manufacturer"].ToString());
                }
            }
            //wait for user action
            Console.ReadLine();
        }


        /// <summary>
        /// 하드웨어 정보
        /// </summary>
        public void getHardwardInfo()
        {
            GetMACAddress();
            Manufacturer();
            this._modelName = Environment.OSVersion.ToString();
            // CPU 함수는 2가지 구현 
            GetProcessor();
            GetMemory();
            GetVGA();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            calcReduction.OperationEndTime = DateTime.Now;

            ////if (IsConnectedToInternet())
            //    this.sendPCEnergy("4");
            this.sendPCEnergy("4");

            calcReduction._OperationStartTime = DateTime.Now;
        }

        #endregion

        #region "메인폼 이벤트"

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
          
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            UIInvoker = new UIInvokerDelegate(DiplayMessage);
            keyHook.MessageHooked += new EventHandler<Hook.KeyboardHookEventArgs>(keyHook_MessageHooked);
            mouseHook.MessageHooked += new EventHandler<Hook.MouseHookEventArgs>(mouseHook_MessageHooked);

            keyHook.StartHook();
            mouseHook.StartHook();

            //시작 프로그램 추가 
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
           
            if (registryKey.GetValue("MyApp") == null)
            {
                registryKey.SetValue("MyApp", Application.ExecutablePath.ToString());
            }
            else if (registryKey.GetValue("MyApp").Equals(@"C:\HansCreative\SSES_Program\SSES_Program.exe"))
            {
                registryKey.SetValue("MyApp", Application.ExecutablePath.ToString());
            }

            getUpTimeSavingTime();

            deviceUserControl1.TbDeviceAddr0.TextChanged += new EventHandler(FocusMove);
            deviceUserControl1.TbDeviceAddr1.TextChanged += new EventHandler(FocusMove);
            deviceUserControl1.TbDeviceAddr2.TextChanged += new EventHandler(FocusMove);
            deviceUserControl1.TbDeviceAddr3.TextChanged += new EventHandler(FocusMove);
            deviceUserControl1.TbDeviceAddr4.TextChanged += new EventHandler(FocusMove);
            deviceUserControl1.TbDeviceAddr5.TextChanged += new EventHandler(FocusMove);

            timer1.Start();
            timer1.Interval = 3600000;

            Application.ApplicationExit +=Application_ApplicationExit;

            //this.etcUserControl1.LinkLabel_etc.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel_etc_LinkClicked);
            this.passwordUserControl1.ChkUserPw1.Click += new System.EventHandler(this.ChkUserPw_Click);
            this.passwordUserControl1.TbUserPw.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbUserPw_KeyDown);
            this.sleepModeUserControl1.ChkMode1.Click += new System.EventHandler(this.ChkMode_Click);
            this.sleepModeUserControl1.RbPcMode.CheckedChanged += new System.EventHandler(this.rbPcMode_CheckedChanged);
            this.sleepModeUserControl1.RbMonitorMode.CheckedChanged += new System.EventHandler(this.rbMonitorMode_CheckedChanged);
            this.deviceUserControl1.BtOk.Click += new System.EventHandler(this.btOk_Click);
            //this.etcUserControl1.BtnFolder.Click += new System.EventHandler(this.button3_Click_1);

            this.deviceUserControl1.RadioButton1.Click += RadioButton1_Click;
            this.deviceUserControl1.RadioButton2.Click += RadioButton2_Click;

            setBtn_Eng();
            setTabCtrl_Eng();

            try
            {
                this.Visible = false;

                this.ShowInTaskbar = true;

                notifyIcon.ShowBalloonTip(300, "SSES", "SSES Program Start", ToolTipIcon.Info);
                // 키보드 후킹 해제
                KeyboardHooking.UnBlockCtrlAltDel();
            }
            catch (Exception error) {
                log.write("MainForm_Load");
                log.write(error.StackTrace);

                MessageBox.Show(error.ToString());
            }
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            //if (IsConnectedToInternet())
            //    this.sendPCEnergy("0");

            this.sendPCEnergy("0");
            //throw new NotImplementedException();
            try
            {
                if (KeyboardHooking.WINDOWSTATUS == KeyboardHooking.SWP_HIDEWINDOW)
                {
                    KeyboardHooking.TaskBarShow();
                }
                // 키보드 후킹 해제
                KeyboardHooking.UnBlockCtrlAltDel();
                Service.AlertSoundStop();
                Console.WriteLine("Main_Close");
                //SSES_Program.Win32.AllowMonitorPowerdown();
            }
            catch (Exception error)
            {
                log.write(error.StackTrace);
                MessageBox.Show(error.ToString());
            }

        }



        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                //if (KeyboardHooking.WINDOWSTATUS == KeyboardHooking.SWP_HIDEWINDOW)
                //{
                   
                //}
                KeyboardHooking.TaskBarShow();
                // 키보드 후킹 해제
                KeyboardHooking.UnBlockCtrlAltDel();
                Console.WriteLine("Main_Close");
                //SSES_Program.Win32.AllowMonitorPowerdown();
            }
            catch (Exception error)
            {
                log.write("MainForm_FormClosed");
                log.write(error.StackTrace);
                Console.WriteLine("Main_Closeerr");
                MessageBox.Show(error.ToString());
            }

        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
        }

        /// <summary>
        /// 폼이 생성되고 나서 최소화 시킴 - 깜빡 거리는 문제 이슈 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion

        #region "버튼 이벤트"

        /// <summary>
        /// 포커스이동
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FocusMove(object sender, EventArgs e)
        {
                TextBox txt = (TextBox)sender;
                if (txt.Text.Length == 2) // 이벤트 핸들러 설정된 컨트롤의 글자입력수가 3글자이면,
                {
                           SendKeys.Send("{tab}"); // Tab키를 실행하고 Focus를 설정. (Tab Order 기준으로 이동함)
                           txt.Focus();
                }
        }

        private void SetButtonColor(int selected)
        {
            Button[] buttons = { button5, button1, button2, button4,  button6 };
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].ForeColor = (i == selected) ? Color.Cyan : Color.White;
            }
        }

        /// <summary>
        /// 회사 소개 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            //tabControl1.SelectedTab = tP5_HomePage;
            try
            {
                System.Diagnostics.Process.Start("http://www.hanscreative.com/");
            }
            catch { }

        }

        /// <summary>
        /// 솔루션 소개 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tP1_IntroHome;
            SetButtonColor(1);
        }

        /// <summary>
        /// 에너지 절감량 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            this.getPCEnergy("", "");
            tabControl1.SelectedTab = tP2_DpEnergySol;
            SetButtonColor(2);
        }

        /// <summary>
        /// 보안 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tP4_SecurityPage;
            SetButtonColor(3);
        }
        
        /// <summary>
        /// 환경 설정 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = this.tP8_SettingPageNew;
            SetButtonColor(4);
        }
        
        #endregion

        #region "tP7_SettingPage"

        private void btOk_Click(object sender, EventArgs e)
        {
            if ((String.IsNullOrEmpty(deviceUserControl1.TbDeviceAddr0.Text)) || (deviceUserControl1.TbDeviceAddr0.TextLength < 2)) { MessageBox.Show(SsesRes.bluetooth_setting_msg + "\n 1st text box"); return; }
            if ((String.IsNullOrEmpty(deviceUserControl1.TbDeviceAddr1.Text)) || (deviceUserControl1.TbDeviceAddr1.TextLength < 2)) { MessageBox.Show(SsesRes.bluetooth_setting_msg + "\n 2nd text box"); return; }
            if ((String.IsNullOrEmpty(deviceUserControl1.TbDeviceAddr2.Text)) || (deviceUserControl1.TbDeviceAddr2.TextLength < 2)) { MessageBox.Show(SsesRes.bluetooth_setting_msg + "\n 3rd text box"); return; }
            if ((String.IsNullOrEmpty(deviceUserControl1.TbDeviceAddr3.Text)) || (deviceUserControl1.TbDeviceAddr3.TextLength < 2)) { MessageBox.Show(SsesRes.bluetooth_setting_msg + "\n 4th text box"); return; }
            if ((String.IsNullOrEmpty(deviceUserControl1.TbDeviceAddr4.Text)) || (deviceUserControl1.TbDeviceAddr4.TextLength < 2)) { MessageBox.Show(SsesRes.bluetooth_setting_msg + "\n 5th text box"); return; }
            if ((String.IsNullOrEmpty(deviceUserControl1.TbDeviceAddr5.Text)) || (deviceUserControl1.TbDeviceAddr5.TextLength < 2)) { MessageBox.Show(SsesRes.bluetooth_setting_msg + "\n 6th text box"); return; }

            bt32FeetDevice.Stop();
            bt32FeetDevice.OnData -= On32FeetData;
            string[] AddArray = { deviceUserControl1.TbDeviceAddr0.Text, deviceUserControl1.TbDeviceAddr1.Text, deviceUserControl1.TbDeviceAddr2.Text, deviceUserControl1.TbDeviceAddr3.Text, deviceUserControl1.TbDeviceAddr4.Text, deviceUserControl1.TbDeviceAddr5.Text };
            DevAddrs = String.Join(":", AddArray);

            AppConfig.Instance.DeviceAddress = DevAddrs;

            MessageBox.Show(SsesRes.deviceAddr_changeMsg, SsesRes.deviceAddr_changeTitle);

            bt32FeetDevice.GetBtAddr(DevAddrs);
            bt32FeetDevice.OnData += On32FeetData;
            bt32FeetDevice.Start();
        }

        private void rbMonitorMode_CheckedChanged(object sender, EventArgs e)
        {
            AppConfig.Instance.SleepMode = 0;
            //MessageBox.Show("모니터 절전모드로 변경되었습니다.", "절전모드 변경완료");
        }

        private void rbPcMode_CheckedChanged(object sender, EventArgs e)
        {
            AppConfig.Instance.SleepMode = 1;
            //MessageBox.Show("모니터+본체 절전모드로 변경되었습니다.", "절전모드 변경완료");
        }

        void RadioButton2_Click(object sender, EventArgs e)
        {
            AppConfig.Instance.Model = 1;
            this.bt32FeetDevice._model = 1;
            //throw new NotImplementedException();
        }

        void RadioButton1_Click(object sender, EventArgs e)
        {
            AppConfig.Instance.Model = 0;
            this.bt32FeetDevice._model = 0;
            //throw new NotImplementedException();
        }

        private void ChkMode_Click(object sender, EventArgs e)
        {
            if (this.sleepModeUserControl1.RbPcMode.Checked) // PC 절전
            {
                MessageBox.Show(SsesRes.sleepMode_changeMsg1, SsesRes.sleepMode_changeTitle);
            }
            else // 모니터 절전 //if (rbMonitorMode.Checked)
            {
                MessageBox.Show(SsesRes.sleepMode_changeMsg0, SsesRes.sleepMode_changeTitle);
            }
        }

        private void DisplayAddToText(string Addr)
        {
            string[] textResult = Addr.Split(':');
            if (textResult != null)
            {
                deviceUserControl1.TbDeviceAddr0.Text = textResult[0];
                deviceUserControl1.TbDeviceAddr1.Text = textResult[1];
                deviceUserControl1.TbDeviceAddr2.Text = textResult[2];
                deviceUserControl1.TbDeviceAddr3.Text = textResult[3];
                deviceUserControl1.TbDeviceAddr4.Text = textResult[4];
                deviceUserControl1.TbDeviceAddr5.Text = textResult[5];
            }
        }
        #endregion

        #region "블루투스 이벤트"
        private void On32FeetData(Bt32FeetDevice sender, string data)
        {
            try
            {                
                if (this.InvokeRequired)
                {
                    this.Invoke((Action)(() => { _Safe_On32FeetData(sender, data); }));
                }
                else
                {
                    if (this.IsHandleCreated)
                    {
                        this.Invoke((Action)(() => { _Safe_On32FeetData(sender, data); }));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        private void _Safe_On32FeetData(Bt32FeetDevice sender, string data)
        {
            try
            {
                log.write("_Safe_On32FeetData");
                ScreenSaver();
            }
            catch (Exception ex)
            {
                Console.WriteLine("eeeeeee");
            }
        }



        // 프로그래스 바 RSSI값 넣기
        void RSSIintoProgressBar()
        {
            if (!this.IsHandleCreated) return;

            Invoke((MethodInvoker)delegate
            {
                //if (!_32FeetDevice.IsConnected)
                //    progressBar1.Style = ProgressBarStyle.Marquee;
                
                //if (_32FeetDevice.IsConnected)
                //{
                //    progressBar1.Style = ProgressBarStyle.Blocks;
                //    progressBar1.MarqueeAnimationSpeed = 20;
                //    progressBar1.Maximum = 120;
                //    progressBar1.Value = rcvRssi + 120;
                //}
                //else
                //{
                //    progressBar1.Style = ProgressBarStyle.Marquee;
                //}

            });
        }
        #endregion

        #region "스크린세이버"
        void ScreenSaver()
        {
            try
            {
                log.write($"IsServiced:{bt32FeetDevice.IsServiced.ToString()}");
                log.write($"screensaverStatus:{screensaverStatus} screensaverPasswordflag:{screensaverPasswordflag} isUserInput:{isUserInput}");
                if (this.bt32FeetDevice.IsServiced == false )  // will be off
                {
                    //화면보호기 시작
                    if (screensaverStatus == false && screensaverPasswordflag == false && isUserInput == false)
                    {
                        calcReduction.OperationEndTime = DateTime.Now;

                        /*if (IsConnectedToInternet())
                            this.sendPCEnergy("2"); */

                        this.sendPCEnergy("2");

                        ScreenSaverSetting();
                        Thread.Sleep(100);
                        screensaverStatus = true;

                        calcReduction.StartTime = DateTime.Now;

                        if (rbPcMode.Checked) // PC 절전
                        {
                            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, false, false);
                        }
                        else // 모니터 절전 //if (rbMonitorMode.Checked)
                        {
                            Service.SendMessage(this.Handle.ToInt32(), Service.WM_SYSCOMMAND, Service.SC_MONITORPOWER, Service.MONITOR_OFF);
                        }
                    }
                }
                else  // will be wake-up 
                {
                    screensaverPasswordflag = false;
                    //스크린 종료
                    if (screensaverStatus == true)
                    {
                        //Console.WriteLine("screen saver END :: {0}", DateTime.Now);

                        /*if (IsConnectedToInternet())
                            this.sendPCEnergy("3");*/

                        this.sendPCEnergy("3");

                        // 컴퓨터 절전해제
                        Service.mouse_event(Service.MOUSE_MOVE, 0, 1, 0, UIntPtr.Zero);
                        Thread.Sleep(40);
                        Service.mouse_event(Service.MOUSE_MOVE, 0, -1, 0, UIntPtr.Zero);

                        calcReduction.EndTime = DateTime.Now;

                        calcReduction._OperationStartTime = DateTime.Now;


                        //화면보호기 종료
                        screenSaverAllStop();
                        Service.AlertSoundStop();

                        screensaverStatus = false;
                        Service.SendMessage(this.Handle.ToInt32(), Service.WM_SYSCOMMAND, Service.SC_MONITORPOWER, Service.MONITOR_ON);

                    }
                }
            }
            catch (Exception error)
            {
                log.write("ScreenSaver");
                log.write(error.StackTrace);

                MessageBox.Show(error.ToString());
            }
        }

        public void SetFormScreenSaver(FormScreenSaver screenSaver)
        {
            try
            {
                this.screenSaver = screenSaver;
            }
            catch (Exception)
            {
                Console.WriteLine("ScreenSaver1");
            }
        }

        public void SetFormScreenSaver2(FormScreenSaver2 screenSaver2)
        {
            try
            {
                this.screenSaver2 = screenSaver2;
            }
            catch(Exception ee)
            {
                Console.WriteLine("ScreenSaver2");
            }
        }

        public void screenSaverAllStop()
        {
            try
            {
                if (screenSaver != null)
                {
                    screenSaver.Close();
                    screenSaver = null;

                    if (screenSaver2 != null)
                    {
                        screenSaver2.Close();
                        screenSaver2 = null;
                    }
                }
                screensaverStatus = false;
                screensaverPasswordflag = true;
                Service.AlertSoundStop();
            }
            catch(Exception ee)
            {
                Console.WriteLine("ScreenAllStop");

            }
        }
        
        void ScreenSaverSetting()
        {
            Screen[] screen = Screen.AllScreens;

            // 듀얼모니터를 사용하지않는 경우
            if (screen.GetLength(0) != 2)
            {
                DualMonitor(screen, 0);
            }
            else // 듀얼모니터를 사용하는 경우
            {
                DualMonitor(screen, 0);
                DualMonitor(screen, 1);
            }
        }

        void DualMonitor(Screen[] screen, int primaryNum)
        {
            try
            {
                Point point;

                int screen1 = 0;
                int screen2 = 1;

                if (screen[primaryNum] == screen[screen1])
                {
                    screenSaver = new FormScreenSaver(this);

                    point = new Point(screen[screen1].Bounds.Location.X, screen[screen1].Bounds.Location.Y);
                    screenSaver.Location = point;

                    //GIF파일의 크기를 메인모니터 크기로 조정
                    screenSaver.pb_screenSaver.Size = new Size(screen[screen1].Bounds.Width, screen[screen1].Bounds.Height);
                    screenSaver.Size = screenSaver.pb_screenSaver.Size;

                    screenSaver.Show(this);

                    //KeyboardHooking.TaskBarHide();
                }
                else
                {
                    screenSaver2 = new FormScreenSaver2(this);

                    point = new Point(screen[screen2].Bounds.Location.X, screen[screen2].Bounds.Location.Y);
                    screenSaver2.Location = point;

                    //GIF파일의 크기를 서브모니터 크기로 조정
                    screenSaver2.pb_screenSaver.Size = new Size(screen[screen2].Bounds.Width, screen[screen2].Bounds.Height);
                    screenSaver2.Size = screenSaver2.pb_screenSaver.Size;

                    screenSaver2.Show(this);
                }
            }
            catch(Exception ee)
            {
                Console.WriteLine("DualMonitor Error");
            }
        }
        #endregion

        #region "트레이아이콘 이벤트"
        private void tools_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = this.tP8_SettingPageNew;
            SetButtonColor(4);

            if (this.WindowState == FormWindowState.Minimized || this.TopMost == false)
            {
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;

                // Activate the form.
                this.Activate();
            }

            if (this.Visible == false) { this.Show(); }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (this.WindowState == FormWindowState.Minimized || this.TopMost == false)
                {
                    this.WindowState = FormWindowState.Normal;
                    this.StartPosition = FormStartPosition.CenterScreen;

                    // Activate the form.
                    this.Activate();
                }

                if (this.Visible == false) { this.Show(); }
            }
            catch (Exception error) {
                log.write("notifyIcon_DoubleClick");
                log.write(error.StackTrace);
                MessageBox.Show(error.ToString()); }
        }
        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
                this.Dispose();
            }
            catch (Exception error) {
                log.write("종료ToolStripMenuItem_Click");
                log.write(error.StackTrace);
                MessageBox.Show(error.ToString()); }
        }

        #endregion

        #region "한/영 Toggle"
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.getPCEnergy("", "");

            if (checkBox_LangToggle.Text.Equals("English"))
            {
                //한->영
                checkBox_LangToggle.Text = "한글";
                SsesRes.Culture = new System.Globalization.CultureInfo("en-US");
                
                setBtnFont("맑은 고딕", 9, FontStyle.Bold);

                setBtn_Eng();
                setTabCtrl_Eng();
                textBlock_cost.Text = String.Format("{0,10:N3}", calcReduction.UsedCost/1200).ToString();
            }
            else
            {
                //영->한
                checkBox_LangToggle.Text = "English";
                SsesRes.Culture = new System.Globalization.CultureInfo("ko-KR");
                setBtnFont("맑은 고딕", 9.75f, FontStyle.Bold);
                setBtn_Eng();
                setTabCtrl_Eng();
                textBlock_cost.Text = String.Format("{0,10:N3}", (calcReduction.UsedCost)).ToString();
                //setBtn_Kor();
                //setTabCtrl_Kor();
            }
        }

        /////
        void setBtn_Eng()
        {
            setBtnFont("맑은 고딕", 9, FontStyle.Bold);

            setControl(button5, new Point(30, 55), 110, 40, SsesRes.hansCreative_introduction);
            setControl(button1, new Point(147, 55), 81, 40, SsesRes.what_Sses);
            setControl(button2, new Point(238, 55), 105, 40, SsesRes.energy_saved);
            setControl(button4, new Point(349, 55), 118, 40, SsesRes.security_worked);
            setControl(button6, new Point(474, 55), 96, 40, SsesRes.sses_configuration);
        }

        void setBtn_Kor()
        {
            setBtnFont("맑은 고딕", 9.75f, FontStyle.Bold);

            setControl(button5, new Point(30, 50), 110, 40, SsesRes.hansCreative_introduction);
            setControl(button1, new Point(147, 50), 81, 40, SsesRes.what_Sses);
            setControl(button2, new Point(238, 50), 105, 40, SsesRes.energy_saved);
            setControl(button4, new Point(349, 50), 118, 40, SsesRes.security_worked);
            setControl(button6, new Point(474, 50), 96, 40, SsesRes.sses_configuration);
        }

        void setBtnFont(string name, float size, FontStyle style)
        {
            Font font = new Font(name, size, style);

            button5.Font = font;
            button1.Font = font;
            button2.Font = font;
            button4.Font = font;
            button6.Font = font;
        }

        void setTabCtrl_Eng()
        {
            //SSES 소개

            textBox.Text = SsesRes.sses_introduction;

            //절감량 및 보안시간
            //_CalcReduc.DevicePerKwh = 160.0;
            calcReduction.OnSaveChanged += (sender2) =>
            {
                /*textBlock_power.Text = String.Format("{0,10:N3}", sender2.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", sender2.UsedCost).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", sender2.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", sender2.Tree).ToString();
                */

                label_dispTotTime.Text = SsesRes.dispTotTime
                    + String.Format(" {0:00}" + SsesRes.day +" {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "";

                //label_dispTotTime.Text = "SSES have kept your PC safe while you have been away for total time period of "
                //    + String.Format("{0:00}day {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "";
            };
            calcReduction.OnSaveChanged(calcReduction);

            label_totTime.Text = "";
            label_totTime1.Text = "";


             //절감량 라벨 폰트 및 위치
             // 1
             setControl(pictureBox4, new Point(28, 25));
             setControl(label1_power, new Point(27, 95), 92, 51, SsesRes.power);
             setControl(textBlock_power, new Point(15, 150));

             // 2
             setControl(pictureBox5, new Point(179, 25));
             setControl(label2_cost, new Point(159, 95), 92, 51, SsesRes.cost);
             setControl(textBlock_cost, new Point(150, 150));

             // 3
             setControl(pictureBox6, new Point(300, 25));
             setControl(label3_co2, new Point(280, 95), 103, 51, SsesRes.co2);
             setControl(textBlock_co2, new Point(268, 150));

             // 4
             setControl(pictureBox7, new Point(400, 25));
             setControl(label4_tree, new Point(410, 95), 100, 34, SsesRes.tree);
             setControl(textBlock_tree, new Point(394, 150));
            
             //환경설정
             label1.Text = SsesRes.pairing;
             label2.Text = SsesRes.mode;
             label3.Text = SsesRes.userPw;
             //label4.Text = SsesRes.etc;
             this.deviceUserControl1.Label1.Text = SsesRes.localName1;

             //setControl(label1, new Point(24, 1), 85, 40, SsesRes.pairing);
             //setControl(label2, new Point(14, 48), 103, 40, SsesRes.mode);
             //setControl(this.deviceUserControl1.Label1, new Point(117, 9), 110, 20, SsesRes.localName1);
             //setControl(label3, new Point(24, 1), 85, 40, SsesRes.userPw);
             //setControl(label4, new Point(14, 48), 103, 40, SsesRes.etc);

             this.sleepModeUserControl1.RbMonitorMode.Text = SsesRes.monitorMode;
             this.sleepModeUserControl1.RbPcMode.Text = SsesRes.pcMode;

             //setControl(this.sleepModeUserControl1.RbMonitorMode, rbMonitorMode.Location, 116, 24, SsesRes.monitorMode);
             //setControl(this.sleepModeUserControl1.RbPcMode, new Point(400, 49), SsesRes.pcMode);

             this.sleepModeUserControl1.ChkMode1.Text = SsesRes.chkMode;
             //setControl(this.sleepModeUserControl1.ChkMode1, new Point(400, 49), SsesRes.chkMode);
           
              this.deviceUserControl1.BtOk.Text = SsesRes.btOk;
            
              this.passwordUserControl1.ChkUserPw1.Text = SsesRes.chkUserPw;


            // 소비전력 및 전기요금 설정
            // this.etcUserControl1.LinkLabel_etc.Text = SsesRes.link_etc;
            // this.etcUserControl1.BtnFolder.Text = SsesRes.folderSec;
            //linkLabel_etc.Text = SsesRes.link_etc;

            /*setControl(label_pairing, new Point(24, 1), 85, 40, SsesRes.pairing);
            setControl(label_Mode, new Point(14, 48), 103, 40, SsesRes.mode);
            setControl(label_localName1, new Point(117, 9), 110, 20, SsesRes.localName1);
            setControl(rbMonitorMode, rbMonitorMode.Location, 116, 24, SsesRes.monitorMode);
            setControl(rbPcMode, new Point(144, 74), 385, 44, SsesRes.pcMode);

            // Confirm button
            btOk.Text = SsesRes.btOk;
            setControl(ChkMode, new Point(400, 49), SsesRes.chkMode);

            // user Password
            label_UserPw.Text = SsesRes.userPw;
            ChkUserPw.Text = SsesRes.chkUserPw;

            // 소비전력 및 전기요금 설정
            label_etc.Text = SsesRes.etc;
            linkLabel_etc.Text = SsesRes.link_etc;
           */


            /*textBox.Text = "SSES keeps your PC safe even when you're away from it and saves its \r\n"
                + "consumption of electricity at the same time. Installing tiny mobile app \r\n"
                + "and small PC agent SW get you very easy and friendly to use this novelty.\r\n"
                + "Protect your PC from abuse and contribute to conservation of earth's \r\n"
                + "enviroment from global climate change. Simply go ahead with the \r\n"
                + "typing 6-byte device address shown on mobile app in the configuration\r\n"
                + "tab of PC agent SW.";

            //절감량 및 보안시간
            //_CalcReduc.DevicePerKwh = 160.0;
            _CalcReduc.OnSaveChanged += (sender2) =>
            {
                textBlock_power.Text = String.Format("{0,10:N3}", sender2.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", sender2.UsedCost).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", sender2.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", sender2.Tree).ToString();


                label_dispTotTime.Text =  SsesRes.dispTotTime
                    + String.Format("{0:00}day {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "";

                //label_dispTotTime.Text = "SSES have kept your PC safe while you have been away for total time period of "
                //    + String.Format("{0:00}day {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "";
            };
            _CalcReduc.OnSaveChanged(_CalcReduc);

            label_totTime.Text = "";
            label_totTime1.Text = "";


           /* //절감량 라벨 폰트 및 위치
            // 1
            setControl(pictureBox4, new Point(28, 25));
            setControl(label1_power, new Point(27, 95), 92, 51, "  Amount of\r\n" + "energy saving\r\n" + "     (kWh)");
            setControl(textBlock_power, new Point(15, 150));

            // 2
            setControl(pictureBox5, new Point(179, 25));
            setControl(label2_cost, new Point(159, 95), 92, 51, " Money from\r\n" + "energy saving\r\n" + "     (USD)");
            setControl(textBlock_cost, new Point(150, 150));

            // 3
            setControl(pictureBox6, new Point(300, 25));
            setControl(label3_co2, new Point(280, 95), 103, 51, "Amount of CO2\r\n" + "    reduction\r\n" + "       (Ton)");
            setControl(textBlock_co2, new Point(268, 150));

            // 4
            setControl(pictureBox7, new Point(400, 25));
            setControl(label4_tree, new Point(410, 95), 100, 34, "Number of trees\r\n" + "  from savings");
            setControl(textBlock_tree, new Point(394, 150));

            //환경설정
            setControl(label_pairing, new Point(24, 1), 85, 40, " Bluetooth\r\nconnection");
            setControl(label_Mode, new Point(14, 48), 103, 40, "   Mode of\r\nenergy saving");
            setControl(label_localName1, new Point(117, 9), 110, 20, "Device address");
            setControl(rbMonitorMode, rbMonitorMode.Location, 116, 24, "Monitor only");
            setControl(rbPcMode, new Point(144, 74), 385, 44, "Monitor together with CPU\r\nThis mode expects any key stroke to unlock your PC");

            // Confirm button
            btOk.Text = "Confirm";
            setControl(ChkMode, new Point(400, 49), "Confirm");

            // user Password
            label_UserPw.Text = "Password";
            ChkUserPw.Text = "Confirm";

            // 소비전력 및 전기요금 설정
            label_etc.Text = "etc";
            linkLabel_etc.Text = "Set power consumption and electricity rate"; */
        }

        void setTabCtrl_Kor()
        {
            textBox.Text = SsesRes.sses_introduction;

            //SSES 소개
           /* textBox.Text = "SSES 는 Smart Security and Energy Saving 의 약자로써, PC와 mobile 간\r\n"
                + "의 블루투스 통신을 이용하여 PC 보안 및 에너지를 절약하는 솔루션입니다.\r\n"
                + "PC와 mobile에 맞는 프로그램을 설치하면 SSES 솔루션을 이용하실 수 있습니다.\r\n"
                + "환경설정에서 mobile 프로그램에 있는 장치주소를 입력한 후, 사용하시기 바랍니다.\r\n\r\n"
                + "감사합니다. :-)"; */

            //
            //_CalcReduc.DevicePerKwh = 160.0;
            calcReduction.OnSaveChanged += (sender2) =>
            {
                textBlock_power.Text = String.Format("{0,10:N3}", sender2.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", sender2.UsedCost).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", sender2.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", sender2.Tree).ToString();
                label_dispTotTime.Text = SsesRes.dispTotTime
                    + String.Format(" {0:00}" + SsesRes.day + " {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "";

                //label_dispTotTime.Text = "SSES 솔루션을 통한 PC의 총 보안시간은 "
                //    + String.Format("{0:00}일 {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "입니다.";
            };
            calcReduction.OnSaveChanged(calcReduction);

            //절감량 라벨 폰트 및 위치
            // 1
            setControl(pictureBox4, new Point(28, 25));
            setControl(label1_power, new Point(27, 115), 110, 12, "에너지절감량(kWh)");
            setControl(textBlock_power, new Point(15, 150));

            // 2
            setControl(pictureBox5, new Point(179, 25));
            setControl(label2_cost, new Point(159, 115), 99, 12, "전기료절감액(원)");
            setControl(textBlock_cost, new Point(150, 150));

            // 3
            setControl(pictureBox6, new Point(300, 25));
            setControl(label3_co2, new Point(300, 115), 75, 12, "CO2절감(톤)");
            setControl(textBlock_co2, new Point(268, 150));

            // 4
            setControl(pictureBox7, new Point(400, 25));
            setControl(label4_tree, new Point(410, 115), 87, 12, "환경보호(그루)");
            setControl(textBlock_tree, new Point(394, 150));
            
            //환경설정
            setControl(label_pairing, new Point(43, 9), 37, 15, "연결");
            setControl(label_Mode, new Point(14, 48), 103, 40, "절전모드 선택");
            setControl(label_localName1, new Point(141, 9), 67, 15, "장치주소");
            setControl(rbMonitorMode, rbMonitorMode.Location, 105, 19, "모니터 절전");
            setControl(rbPcMode, new Point(144, 74), 143, 19, "모니터+본체 절전");

            // 입력/확인 버튼
            btOk.Text = "입력";
            setControl(ChkMode, new Point(320, 49), "확인");

            // user Password
            label_UserPw.Text = "비밀번호";
            ChkUserPw.Text = "확인";

            // 소비전력 및 전기요금 설정
            label_etc.Text = "기타";
            linkLabel_etc.Text = "소비전력 및 전기요금 설정";
        }
        

        private void setControl(Control ctrl, Point location)
        {
            ctrl.Location = location;
        }

        private void setControl(Control ctrl, Point location, string text)
        {
            ctrl.Location = location;
            ctrl.Text = text;
        }

        private void setControl(Control ctrl, Point location, int width, int height)
        {
            ctrl.Location = location;
            ctrl.Width = width;
            ctrl.Height = height;
        }

        private void setControl(Control ctrl, Point location, int width, int height, string text)
        {
            ctrl.Text = text;
            ctrl.Location = location;
            ctrl.Width = width;
            ctrl.Height = height;
        }

        #endregion

        #region 비밀번호 설정

        private void ChkUserPw_Click(object sender, EventArgs e)
        {
            if (this.passwordUserControl1.TbUserPw.Text != "")
            {
                userPw = this.passwordUserControl1.TbUserPw.Text;
                AppConfig.Instance.UserPassword = userPw;

                MessageBox.Show("비밀번호가 변경되었습니다.", "비밀번호 변경완료");
            }
            else
            {
                MessageBox.Show("다시 입력해주세요", "비밀번호 재설정");
            }

            this.passwordUserControl1.TbUserPw.Clear();
        }

        private void tbUserPw_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ChkUserPw_Click(sender, e);
            }
        }

        #endregion

        #region "기타"
        private void linkLabel_etc_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SettingPopup popupForm = new SettingPopup();
            popupForm.Owner = this;
            popupForm.Popup_BtnClickEvent += PopupForm_Popup_BtnClickEvent;
            popupForm.ShowDialog();
        }

        private void PopupForm_Popup_BtnClickEvent(double power, double bill)
        {
            //throw new NotImplementedException();

            ratedOutput_device = power; // 변수에 저장 (변수 왜 있는지 모르겠음)

            AppConfig.Instance.PcPower = power; // 파일에 저장 (set)
            AppConfig.Instance.ElecRate = bill; // 파일에 저장 (set)
            
            calcReduction.DevicePerKwh = power; // 변수에 값 대입
            calcReduction.WonPerKwh = bill; // 변수에 값 대입

            //_CalcReduc.Calculate(); // 절감량 계산
            calcReduction.OnSaveChanged += (sender2) =>
            {
                /*textBlock_power.Text = String.Format("{0,10:N3}", sender2.UsedKwh).ToString();
                textBlock_cost.Text = String.Format("{0,10:N3}", sender2.UsedCost).ToString();
                textBlock_co2.Text = String.Format("{0,10:N3}", sender2.Co2).ToString();
                textBlock_tree.Text = String.Format("{0,10:N3}", sender2.Tree).ToString();
                */

                if (checkBox_LangToggle.Text.Equals("English"))
                {
                    label_dispTotTime.Text = "SSES 솔루션을 통한 PC의 총 보안시간은 "
                    + String.Format("{0:00}일 {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "입니다.";
                }
                else
                {
                    label_dispTotTime.Text = "SSES have kept your PC safe while you have been away for total time period of "
                    + String.Format("{0:00}day {1:00}:{2:00}:{3:00}", sender2.UsedSec.Days, Math.Floor(sender2.UsedSec.TotalHours), sender2.UsedSec.Minutes, sender2.UsedSec.Seconds).ToString() + "";
                }
            };
            calcReduction.OnSaveChanged(calcReduction);

        }
        #endregion 

        #region "사용 안하는 함수들 (정리 예정)"
        #region TrackBar_Event
        // 트랙바 이벤트
        private void trackBar_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyValue)
            {
                case (int)Keys.Left:
                    trackBar.Value += 1;
                    //button3.Focus();
                    break;
                case (int)Keys.Right:
                    trackBar.Value -= 1;
                    //button3.Focus();
                    break;
                case (int)Keys.Up:
                    trackBar.Value += 1;
                    //button3.Focus();
                    break;
                case (int)Keys.Down:
                    trackBar.Value -= 1;
                    //button3.Focus();
                    break;
                case (int)Keys.Escape:
                default:
                    //button3.Focus();
                    break;
            }

        }

        // 트랙바 이벤트
        private void trackBar_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trackBar, "현재 : " + trackBar.Value.ToString());
        }

        // 트랙바 이벤트
        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (trackBar.Value <= -60 && trackBar.Value >= -90)
                {
                    toolTip1.SetToolTip(trackBar, trackBar.Value.ToString());
                }
                else if (trackBar.Value > -60)
                {
                    trackBar.Value = -60;
                    toolTip1.SetToolTip(trackBar, trackBar.Value.ToString() + " 더 이상 증가시킬 수 없습니다.");
                }
                else if (trackBar.Value < -90)
                {
                    trackBar.Value = -90;
                    toolTip1.SetToolTip(trackBar, trackBar.Value.ToString() + " 더 이상 감소시킬 수 없습니다.");
                }
                userRssi = trackBar.Value;
                AppConfig.Instance.TrackBar = userRssi;
                //textBox_Trackbar.Text = userRssi.ToString();

            }
            catch (Exception error) { MessageBox.Show(error.ToString()); }
        }
        #endregion

        public enum RadioMode
        {
            Off = 0,
            Connectable = 1,
            Discoverable = 2
        }

        [DllImport("BthUtil.dll")]
        public static extern int BthGetMode(out RadioMode dwMode);

        [DllImport("BthUtil.dll")]
        public static extern int BthSetMode(RadioMode dwMode);

        private static void setBluetoothConnection()
        {
            try
            {
                if (BluetoothRadio.IsSupported == true)
                {
                    MessageBox.Show("Bluetooth Supported", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    BluetoothRadio radio = BluetoothRadio.PrimaryRadio;
                    MessageBox.Show(radio.Mode.ToString(), "Before Bluetooth Connection", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);


                    BluetoothClient bluetoothClient = new BluetoothClient();

                    BluetoothDeviceInfo[] bluetoothDeviceInfo = bluetoothClient.DiscoverDevices();
                    MessageBox.Show(bluetoothDeviceInfo.Length.ToString(), "Device Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    foreach (BluetoothDeviceInfo device in bluetoothDeviceInfo)
                    {
                        Cursor.Current = Cursors.Default;
                        MessageBox.Show(device.DeviceName, "Device Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        //bluetoothClient.Connect(new BluetoothEndPoint(device.DeviceAddress, service));
                        MessageBox.Show("Bluetooth Connected...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        break;
                    }

                    /*radio.Mode = RadioMode.Discoverable;
                    // here radio.Mode works only if the Windows Device has Bluetooth enabled otherwise gives error
                    MessageBox.Show(radio.Mode.ToString(), "RadioMode Discover", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    bluetoothClient = new BluetoothClient();
                    //Cursor.Current = Cursors.WaitCursor;
                    BluetoothDeviceInfo[] bluetoothDeviceInfo = bluetoothClient.DiscoverDevices();
                    MessageBox.Show(bluetoothDeviceInfo.Length.ToString(), "Device Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    foreach(BluetoothDeviceInfo device in bluetoothDeviceInfo)
                    {
                      Cursor.Current = Cursors.Default;
                      MessageBox.Show(device.DeviceName, "Device Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                      bluetoothClient.Connect(new BluetoothEndPoint(device.DeviceAddress, service));
                      MessageBox.Show("Bluetooth Connected...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                      break;
                    }*/
                }
                else
                {
                    MessageBox.Show("Bluetooth Not Supported", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception ex)
            {
              
                //log.Error("[Bluetooth] Connection failed", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("bthprops.cpl");

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 최소화 버튼 기능 추가 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMinimum_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion
    }
}

