﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace HTTP_modifier
{
    class Logs
    {
        private ListView logsListView;

        public Logs(ListView logsListView)
        {
            this.logsListView = logsListView;
        }


        public void addLog(string log, bool time, int flag, bool anotherThread = false)
        {
            ListViewItem item = new ListViewItem();
            switch (flag)
            {
                case 0:
                    item.ForeColor = Color.Blue;
                    break;
                case 1:
                    item.ForeColor = Color.Black;
                    break;
                case 2:
                    item.ForeColor = Color.Red;
                    break;
                case 3:
                    item.ForeColor = Color.Green;
                    break;
            }

            if (time)
                item.Text = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + log;
            else
                item.Text = log;

            if (!anotherThread)
            {
                logsListView.Items.Add(item);
                logsListView.Items[logsListView.Items.Count - 1].EnsureVisible();
            }
            else
            {
                logsListView.Invoke(new MethodInvoker(delegate()
                {
                    logsListView.Items.Add(item);
                    logsListView.Items[logsListView.Items.Count - 1].EnsureVisible();
                })
                    );
            }
        }

    }
}
