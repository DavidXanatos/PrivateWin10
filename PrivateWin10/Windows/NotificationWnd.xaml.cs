﻿using PrivateWin10.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PrivateWin10.Windows
{
    public interface INotificationTab
    {
        event EventHandler<EventArgs> Emptied;

        void Closing();
        bool IsEmpty();
    }


    /// <summary>
    /// Interaction logic for NotificationWnd.xaml
    /// </summary>
    public partial class NotificationWnd : Window
    {

        DispatcherTimer mTimer = new DispatcherTimer();

        private bool close = false;

        class STab
        {
            public INotificationTab notify;
            public Image image;
            public enum EState {
                eEmpty,
                eFilled,
                eNew
            }
            public EState state = STab.EState.eEmpty;
        }

        Dictionary<TabItem, STab> tabItems = new Dictionary<TabItem, STab>();

        public NotificationWnd(bool HasEngine)
        {
            InitializeComponent();

            this.Title = Translate.fmt("wnd_notify");
            this.Topmost = true;

            this.conTab.Header = Translate.fmt("lbl_con_notify");
            tabItems.Add(this.conTab, new STab() { notify = this.ConNotify, image = this.imgCon });
            this.ruleTab.Header = Translate.fmt("lbl_rule_notify");
            tabItems.Add(this.ruleTab, new STab() { notify = this.RuleNotify, image = this.imgRule });
            //this.tweakTab.Header = Translate.fmt("lbl_tweak_notify");
            //tabItems.Add(this.tweakTab, new STab() { notify = this.TweakNotify, image = this.imgTweak });

            foreach (var item in tabItems) {
                item.Value.notify.Emptied += OnEmptied;
                item.Key.IsEnabled = false;
            }

            // hide firewall tabs if encinge is no available
            if (!HasEngine)
            {
                this.tabs.SelectedIndex = 2;
                this.conTab.Visibility = Visibility.Collapsed;
                this.ruleTab.Visibility = Visibility.Collapsed;
            }

            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            mTimer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            foreach(var item in tabItems)
            {
                switch (item.Value.state)
                {
                    case STab.EState.eEmpty:
                        if (item.Value.image.Visibility != Visibility.Hidden)
                            item.Value.image.Visibility = Visibility.Hidden;
                        break;
                    case STab.EState.eFilled:
                        if (item.Value.image.Visibility != Visibility.Visible)
                            item.Value.image.Visibility = Visibility.Visible;
                        break;
                    case STab.EState.eNew:
                        if (item.Value.image.Visibility != Visibility.Visible)
                            item.Value.image.Visibility = Visibility.Visible;
                        else if (item.Value.image.Visibility != Visibility.Hidden)
                            item.Value.image.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!close)
            {
                e.Cancel = true;
                HideWnd();
                return;
            }

            foreach (var item in tabItems)
                item.Value.notify.Closing();
        }

        public void OnEmptied(object sender, EventArgs e)
        {
            tabs.SelectedItem = null;
            foreach (var item in tabItems)
            {
                if (item.Value.notify.IsEmpty())
                {
                    item.Key.IsEnabled = false;
                    item.Value.state = STab.EState.eEmpty;
                }
                else if (tabs.SelectedItem == null)
                    tabs.SelectedItem = item.Key;
            }

            if(tabs.SelectedItem == null)
                HideWnd();
        }

        public void AddCon(ProgramSet prog, Priv10Engine.FwEventArgs args)
        {
            this.conTab.IsEnabled = true;
            tabs.SelectedItem = this.conTab;
            tabItems[this.conTab].state = STab.EState.eNew;
            //tabItems[this.conTab].state = tabs.SelectedItem != this.conTab ? STab.EState.eNew : STab.EState.eFilled;
            if (this.ConNotify.Add(prog, args))
                ShowWnd();
        }

        public void NotifyRule(Priv10Engine.ChangeArgs args)
        {
            this.ruleTab.IsEnabled = true;
            tabs.SelectedItem = this.ruleTab;
            tabItems[this.ruleTab].state = STab.EState.eNew;
            //tabItems[this.ruleTab].state = tabs.SelectedItem != this.ruleTab ? STab.EState.eNew : STab.EState.eFilled;
            if (this.RuleNotify.Add(args))
                ShowWnd();
        }

        public void NotifyTweak(TweakManager.TweakEventArgs args)
        {
            /*this.tweakTab.IsEnabled = true;
            tabs.SelectedItem = this.tweakTab;
            tabItems[this.tweakTab].state = STab.EState.eNew;
            //tabItems[this.tweakTab].state = tabs.SelectedItem != this.tweakTab ? STab.EState.eNew : STab.EState.eFilled;
            if (this.TweakNotify.Add(args))
                ShowWnd();*/
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = tabs.SelectedItem as TabItem;
            if (item == null || tabItems.Count == 0)
                return;
            if (tabItems[item].state == STab.EState.eNew)
                tabItems[item].state = STab.EState.eFilled;
        }

        public void ShowWnd()
        {
            if (!WpfFunc.LoadWnd(this, "Notify"))
            {
                this.Left = SystemParameters.WorkArea.Width - this.Width - 4.0;
                this.Top = SystemParameters.WorkArea.Height - this.Height - 4.0;
            }

            Show();
        }

        public void HideWnd()
        {
            WpfFunc.StoreWnd(this, "Notify");

            Hide();
        }

        public void CloseWnd()
        {
            close = true;

            this.Close();
        }

        public bool IsEmpty()
        {
            return tabs.SelectedItem == null;
        }
    }
}
