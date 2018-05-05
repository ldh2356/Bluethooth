using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Threading;
using InTheHand.Net.Bluetooth.AttributeIds;

using System.Windows.Forms;
using System.Diagnostics;

namespace DeviceAgents
{
    public class Bt32FeetDevice : DeviceBase
    {
        static string addrs;
        static BluetoothAddress addr;
        static BluetoothDeviceInfo bdi;
        private int _lockcount;
        IAsyncResult ir;
        private ConnectLog log = new ConnectLog();


        public int LockCount
        {
            get 
            {
                return _lockcount;
            }
            set
            {
                _lockcount = value;
            }
        }
        public int _model;   
        
        
        static string uuidStr = "00002415-0000-1000-8000-00805F9B34FB";
        Guid uuid = new Guid(uuidStr);
        
        public Bt32FeetDevice()
        {
        }

        public void GetBtAddr(string divAddr)
        {
            addrs = divAddr;
        }

        public override void Dispose()
        {
            Stop();
            base.Dispose();
        }
        

        //---------------------------------------------------------------------

        public override void Init()
        {
        }
        //---------------------------------------------------------------------
        bool _IsConnected = false;
        protected override bool IsConnectedImpl()
        {
            return _IsConnected;
        }
        //---------------------------------------------------------------------
        bool _IsServiced = true;
        protected override bool IsServicedImpl()
        {
            return _IsServiced;
        }
        //---------------------------------------------------------------------
        protected override void DoWork()
        {
            try
            {
                addr = BluetoothAddress.Parse(addrs);

                // 블루투스 확인, (블루투스를 킬 때까지 장치 제어판 계속 띄워줌)

                while (true)
                {
                    if (!BluetoothRadio.IsSupported)
                    {
                        Process.Start("bthprops.cpl");

                        //2017.11.29일 - 2차 기능 정의서의 내용중 블뤁추스 장치가 없을 경우 무한 반복되는 메시지 창을 수정
                        if (MessageBox.Show(Device.bluetoothOffMsg, "SSES", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                        {
                            break;
                        }
                    }
                    else
                    {
                        bdi = new BluetoothDeviceInfo(addr);
                        break;
                    }
                }

                while (true)
                {

                    ir = bdi.BeginGetServiceRecords(uuid, Service_AsyncCallback, bdi);

                    Console.WriteLine(" uuid  ====> " + uuid);
                    if (!_IsServiced)
                    {
                        if (OnData != null)
                            OnData(this, "-95");
                    }
                    else
                    {
                        if (OnData != null)
                            OnData(this, "-45");
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " Thread Exception!!! 1");
            }
        }

        private void Service_AsyncCallback(IAsyncResult result)
        {
            try
            {
                bdi = result.AsyncState as BluetoothDeviceInfo;
                if (result.IsCompleted)
                {
                  
                    _IsConnected = true;

                    if (_model == 0)
                    {
                        ServiceRecord[] services = bdi.EndGetServiceRecords(result);
                        if (services.Length != 0)
                        {
                            _IsServiced = true;
                            int ind = 0;
                            _lockcount = 0;
                            foreach (ServiceRecord r in services)
                            {
                                int port = ServiceRecordHelper.GetRfcommChannelNumber(r);
                                string curSvcName = r.GetPrimaryMultiLanguageStringAttributeById(UniversalAttributeId.ServiceName);
                                Console.WriteLine("{0} : port={1} Name={2}", ind, port, curSvcName);
                                ind++;
                            }
                        }
                        else
                        {
                            log.write("Android DisConnect");
                            Console.WriteLine("services.Length ==> 0");
                            _lockcount++;
                             if (_lockcount > 3)
                             {
                                _IsServiced = false;
                             }
                        }
                    }
                    else
                    {
                        try
                        {
                            ServiceRecord[] services = bdi.EndGetServiceRecords(result);
                            Console.WriteLine("IOS Service_AsyncCallback");
                            _IsServiced = true;
                            _lockcount = 0;
                        }
                        catch(Exception ea)
                        {
                            log.write("IOS DisConnect");
                            Console.WriteLine(ea.Message + "IOS Exception");
                            _lockcount++;
                            if(_lockcount >  10)
                            {
                                _IsConnected = false;
                                _IsServiced = false;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("result.IsCompleted false");
                    _IsConnected = false;
                    _IsServiced = false;
                }
            }
            catch(Exception ea)
            {
                Console.WriteLine(ea.ToString() + DateTime.Now.ToString());
                _IsConnected = false;
                _IsServiced = false;
            }
        }
    }
}
