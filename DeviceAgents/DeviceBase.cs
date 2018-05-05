using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.ComponentModel;

namespace DeviceAgents
{
    public delegate void DeviceDataHandler(DeviceBase device, string data);

    public class DeviceBase : IDisposable
    {
        protected Thread _Worker = null;

        private BackgroundWorker background = new BackgroundWorker();
        public DeviceDataHandler OnData = null;

        public DeviceBase()
        {
        }

        public virtual void Dispose()
        {
            //throw new NotImplementedException();
        }

        public virtual void Init()
        {
        }

        public virtual void Start()
        {
            _Worker = new Thread(DoWork);
            _Worker.Start();
            //background.WorkerSupportsCancellation = true;
            //background.DoWork += new DoWorkEventHandler(bw_DoWork);
            //background.RunWorkerAsync();
        }

        public virtual void Stop()
        {
            if (_Worker == null) return;
            _Worker.Interrupt();
            _Worker = null;
            //if (background.WorkerSupportsCancellation == true)
            //{
            //    background.CancelAsync();
            //}
        }

        protected virtual bool IsConnectedImpl()
        {
            return false;
        }

        public bool IsConnected
        {
            get { return IsConnectedImpl(); }
        }

        protected virtual bool IsServicedImpl()
        {
            return false;
        }

        public bool IsServiced
        {
            get { return IsServicedImpl(); }
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DoWork();
        }

        protected virtual void DoWork()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " Thread Exception!!!");
            }
        }

    }
}
