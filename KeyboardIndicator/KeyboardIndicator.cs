using System;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using System.Media;
using System.Collections.Generic;
using Microsoft.Win32;
using SimpleOSD;
using KeyHook;
using AnimateWindowFlags = SimpleOSD.WinApi.AnimateWindowFlags;


namespace KeyboardIndicator
{
    public partial class KeyboardIndicator : Form
    {
        static AssemblyCopyrightAttribute copyright = Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute;

        static AssemblyDescriptionAttribute desc = Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute;

        private readonly string r_AboutString = string.Format("{0} {1}\n{2}\n{3}",
            Application.ProductName,
            Application.ProductVersion,
            copyright.Copyright,
            desc.Description);

        private const bool k_ResetColors = true;
        private string m_OsdString;
        private eLocation m_OsdLocation = eLocation.BottomRight;
        private SimpleOsdForm m_OSD = new SimpleOsdForm();
        private SimpleOsdForm m_ExampleOSD = null;
        private const int k_Margin = 3;
        private Keys m_LastKey = Keys.None;
        private SoundPlayer m_SoundPlayer = null;
        private bool m_EnableSound = true;
        private AnimateWindowFlags m_HideAnimation = AnimateWindowFlags.AW_CENTER;
        private eStyle m_Style = eStyle.Normal;
        
        KeyboardHook m_KBH = new KeyboardHook();

        private Icon[] m_IndicatorIcons = new Icon[] {
            Icon.FromHandle(Properties.Resources.CapsLockOff.GetHicon()),
            Icon.FromHandle(Properties.Resources.CapsLockOn.GetHicon()),
            Icon.FromHandle(Properties.Resources.NumLockOff.GetHicon()),
            Icon.FromHandle(Properties.Resources.NumLockOn.GetHicon()),
            Icon.FromHandle(Properties.Resources.ScrollLockOff.GetHicon()),
            Icon.FromHandle(Properties.Resources.ScrollLockOn.GetHicon())
        };

        public enum eStyle
        {
            Normal,
            StickyHorizontal,
            StickyVertical
        }

        private enum eIconIndex : int
        {
            CapsLockOff,
            CapsLockOn,
            NumLockOff,
            NumLockOn,
            ScrollLockOff,
            ScrollLockOn
        }

        public KeyboardIndicator()
        {
            InitializeComponent();
            
            fillAnimationCb();
            fillStyleCb();
            lblAbout.Text = r_AboutString;
        }

        private void fillStyleCb()
        {
            List<KeyValuePair<string, eStyle>> l = new List<KeyValuePair<string, eStyle>>();
            l.Add(new KeyValuePair<string, eStyle>("Normal", eStyle.Normal));
            l.Add(new KeyValuePair<string, eStyle>("Sticky Horizontal", eStyle.StickyHorizontal));
            l.Add(new KeyValuePair<string, eStyle>("Sticky Vertical", eStyle.StickyVertical));
            cbStyle.DataSource = l;
            cbStyle.ValueMember = "Key";
        }

        private void fillAnimationCb()
        {
            List<KeyValuePair<string, AnimateWindowFlags>> l = new List<KeyValuePair<string, AnimateWindowFlags>>();
            l.Add(new KeyValuePair<string, AnimateWindowFlags>("Center", AnimateWindowFlags.AW_CENTER));
            l.Add(new KeyValuePair<string, AnimateWindowFlags>("Blend", AnimateWindowFlags.AW_BLEND));
            l.Add(new KeyValuePair<string, AnimateWindowFlags>("Horizontal Negative", AnimateWindowFlags.AW_HOR_NEGATIVE));
            l.Add(new KeyValuePair<string, AnimateWindowFlags>("Horizontal Positive", AnimateWindowFlags.AW_HOR_POSITIVE));
            l.Add(new KeyValuePair<string, AnimateWindowFlags>("Vertical Negative", AnimateWindowFlags.AW_VER_NEGATIVE));
            l.Add(new KeyValuePair<string, AnimateWindowFlags>("Vertical Positive", AnimateWindowFlags.AW_VER_POSITIVE));
            cbAnimation.DataSource = l;
            cbAnimation.ValueMember = "Key";
        }

        private void selectAnimationCb(AnimateWindowFlags i_Animation)
        {
            foreach (KeyValuePair<string, AnimateWindowFlags> item in cbAnimation.Items)
            {
                if (item.Value == i_Animation)
                {
                    cbAnimation.SelectedItem = item;
                    break;
                }
            }
        }

        private void selectStyleCb(eStyle i_Style)
        {
            foreach (KeyValuePair<string, eStyle> item in cbStyle.Items)
            {
                if (item.Value == i_Style)
                {
                    cbStyle.SelectedItem = item;
                    break;
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            hideMyself();
            loadSettings();
            m_KBH.KeyDownIntercepted += new KeyboardHook.KeyboardHookEventHandler(KBH_KeyDownIntercepted);
            m_KBH.KeyUpIntercepted += new KeyboardHook.KeyboardHookEventHandler(KBH_KeyUpIntercepted);
        }

        private void updateModifiers()
        {
            updateCapsLock(KeyboardState.CapsLock);
            updateNumLock(KeyboardState.NumLock);
            updateScrollLock(KeyboardState.Scroll);
        }

        void KBH_KeyUpIntercepted(KeyboardHook.KeyboardHookEventArgs e)
        {
            m_LastKey = Keys.None;
        }

        private void KBH_KeyDownIntercepted(KeyboardHook.KeyboardHookEventArgs e)
        {
            // note: key not intercepted yet by the os
            // e.g. caps lock press from off to on still gives False

            if (e.Key != m_LastKey)
            {
                switch (e.Key)
                {
                    case Keys.CapsLock:
                        updateCapsLock(!KeyboardState.CapsLock);
                        ShowOSD();
                        break;

                    case Keys.NumLock:
                        updateNumLock(!KeyboardState.NumLock);
                        ShowOSD();
                        break;

                    case Keys.Scroll:
                        updateScrollLock(!KeyboardState.Scroll);
                        ShowOSD();
                        break;

                    case Keys.Insert:
                        updateInsert();
                        ShowOSD();
                        break;
                }

                m_LastKey = e.Key;
            }
        }

        private void updateCapsLock(bool i_Toggle)
        {
            string text = i_Toggle ? tbCapsLockOn.Text : tbCapsLockOff.Text;
            m_OsdString = text;
            notifyIconCapsLock.Icon = i_Toggle ? m_IndicatorIcons[(int)eIconIndex.CapsLockOn]
                                    : m_IndicatorIcons[(int)eIconIndex.CapsLockOff];
            notifyIconCapsLock.Text = text;
            notifyIconCapsLock.BalloonTipText = text;
            m_ListLabels[(int)eOSD.CapsLock] = text;
        }

        private void updateNumLock(bool i_Toggle)
        {
            string text = i_Toggle ? tbNumLockOn.Text : tbNumLockOff.Text;
            m_OsdString = text;
            notifyIconNumLock.Icon = i_Toggle ? m_IndicatorIcons[(int)eIconIndex.NumLockOn]
                                    : m_IndicatorIcons[(int)eIconIndex.NumLockOff];
            notifyIconNumLock.Text = text;
            notifyIconNumLock.BalloonTipText = text;
            m_ListLabels[(int)eOSD.NumLock] = text;
        }

        private void updateScrollLock(bool i_Toggle)
        {
            string text = i_Toggle ? tbScrollLockOn.Text : tbScrollLockOff.Text;
            m_OsdString = text;
            notifyIconScrollLock.Icon = i_Toggle ? m_IndicatorIcons[(int)eIconIndex.ScrollLockOn]
                                    : m_IndicatorIcons[(int)eIconIndex.ScrollLockOff];
            notifyIconScrollLock.Text = text;
            notifyIconScrollLock.BalloonTipText = text;
            m_ListLabels[(int)eOSD.ScrollLock] = text;
        }

        private void updateInsert()
        {
            m_OsdString = tbInsert.Text;
        }

        private void ShowOSD()
        {
            if (m_Style == eStyle.Normal && m_OsdString != string.Empty)
            {
                m_OSD.Label.Text = m_OsdString;
                Point p = new Point();
                p.X = SystemInformation.VirtualScreen.X;
                p.Y = SystemInformation.VirtualScreen.Y;
                //this.m_OSD.Location = new Point(p.X, p.Y);
                updateLocation();
                m_OSD.Show();
            }
            else
            {
                updateStickyOSD();
            }
            playSound();
        }

        private enum eOSD : int
        {
            CapsLock,
            NumLock,
            ScrollLock
        }

        private List<string> m_ListLabels =
            new List<string>(3) { string.Empty, string.Empty, string.Empty };

        private List<SimpleOsdForm> m_ListStickyOSD = new List<SimpleOsdForm>(3);

        private void updateStickyOSD()
        {
            // style normal - hide and return
            if (m_Style == eStyle.Normal)
            {
                foreach (SimpleOsdForm o in m_ListStickyOSD)
                    o.HideFast();
                return;
            }

            // initialize osds
            if (m_ListStickyOSD.Count == 0)
            {
                for (int c = 0; c < 3; c++)
                {
                    SimpleOsdForm o = new SimpleOsdForm();
                    applySettingsToOSD(o);
                    m_ListStickyOSD.Add(o);
                }
            }

            int width = 0;
            int height = 0;
            int active = 0;

            foreach (eOSD o in Enum.GetValues(typeof(eOSD)))
            {
                int i = (int)o;

                if (m_ListLabels[i] == string.Empty)
                {
                    m_ListStickyOSD[i].HideFast();
                    continue;
                }

                active++;

                m_ListStickyOSD[i].Label.AutoSize = true; // this fix width and height
                m_ListStickyOSD[i].Label.Text = m_ListLabels[i];
                
                // get max width
                if (m_ListStickyOSD[i].Label.Width > width)
                    width = m_ListStickyOSD[i].Label.Width;

                if (height == 0)
                    height = m_ListStickyOSD[i].Label.Height;

                m_ListStickyOSD[i].ShowAlways();
            }

            Point p = m_OSD.Location;

            if (m_OsdLocation == eLocation.BottomRight ||
                m_OsdLocation == eLocation.UpperRight)
            {
                p.X = Screen.PrimaryScreen.WorkingArea.Width - width - k_Margin;
            }

            int horizontal_width = active * width + active * k_Margin;
            int hz_half_width = horizontal_width / 2;

            if (m_Style == eStyle.StickyHorizontal &&
                (m_OsdLocation == eLocation.BottomRight ||
                 m_OsdLocation == eLocation.UpperRight))
            {
                p.X = Screen.PrimaryScreen.WorkingArea.Width - horizontal_width;
            }

            int vertical_height = active * height + active * k_Margin;
            int vt_half_height = vertical_height / 2;

            if (m_Style == eStyle.StickyVertical &&
                (m_OsdLocation == eLocation.BottomLeft ||
                 m_OsdLocation == eLocation.BottomRight))
            {
                p.Y = Screen.PrimaryScreen.WorkingArea.Height - vertical_height;
            }

            if (m_OsdLocation == eLocation.Center)
            {
                if (m_Style == eStyle.StickyHorizontal)
                    p.X = p.X - hz_half_width;
                else if (m_Style == eStyle.StickyVertical)
                    p.Y = p.Y - vt_half_height;
            }

            m_OSD.Location = p;

            foreach (eOSD o in Enum.GetValues(typeof(eOSD)))
            {
                int i = (int)o;

                if (m_ListLabels[i] == string.Empty)
                    continue;

                m_ListStickyOSD[i].Location = p;

                switch (m_Style)
                {
                    case eStyle.StickyHorizontal:
                        //p.X = p.X + m_ListStickyOSD[i].Label.Width + k_Margin;
                        p.X = p.X + width + k_Margin;
                        break;

                    case eStyle.StickyVertical:
                        p.Y = p.Y + m_ListStickyOSD[i].Label.Height + k_Margin;
                        break;
                }
            }

            foreach (SimpleOsdForm o in m_ListStickyOSD)
            {
                if (width > 0)
                {
                    // fixed width for sticky vertical
                    o.Label.AutoSize = false;
                    o.Label.Width = width;
                }
            }
        }

        private void playSound()
        {
            if (m_EnableSound && m_SoundPlayer != null)
            {
                m_SoundPlayer.Play();
            }
        }

        private void updateLocation()
        {
            switch (m_OsdLocation)
            {
                case eLocation.BottomLeft:
                    setBottomLeft(m_OSD);
                    break;

                case eLocation.BottomRight:
                    setBottomRight(m_OSD);
                    break;

                case eLocation.UpperLeft:
                    setUpperLeft(m_OSD);
                    break;
                
                case eLocation.UpperRight:
                    setUpperRight(m_OSD);
                    break;

                case eLocation.Center:
                    m_OSD.CenterToScreen();
                    break;

                case eLocation.Manual:
                    m_OSD.Location = Properties.Settings.Default.OsdPoint;
                    break;
            }
        }

        private enum eLocation
        {
            BottomLeft,
            BottomRight,
            UpperLeft,
            UpperRight,
            Center,
            Manual
        }

        private void setBottomLeft(SimpleOsdForm i_Osd)
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int x = workingArea.Left + k_Margin;
            int y = workingArea.Bottom - i_Osd.Height - k_Margin;
            i_Osd.Location = new Point(x, y);
        }

        private void setBottomRight(SimpleOsdForm i_Osd)
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int x = workingArea.Right - i_Osd.Width - k_Margin;
            int y = workingArea.Bottom - i_Osd.Height - k_Margin;
            i_Osd.Location = new Point(x, y);
        }

        private void setUpperLeft(SimpleOsdForm i_Osd)
        {
            i_Osd.Location = new Point(k_Margin, k_Margin);
        }

        private void setUpperRight(SimpleOsdForm i_Osd)
        {
            setBottomRight(i_Osd);
            i_Osd.Location = new Point(i_Osd.Location.X, k_Margin);
        }

        private void buttonForeColor_Click(object sender, EventArgs e)
        {
            colorDialog.Color = m_ExampleOSD.Label.ForeColor;

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                m_ExampleOSD.Label.ForeColor = colorDialog.Color;
            }
        }

        private void buttonBackColor_Click(object sender, EventArgs e)
        {
            colorDialog.Color = m_ExampleOSD.Label.BackColor;

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                m_ExampleOSD.Label.BackColor = colorDialog.Color;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            hideMyself();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            saveSettings();
            updateModifiers();
            applySettings();
            updateStickyOSD();
            hideMyself();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            showMyself();
        }

        private void showMyself()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            loadExample();
            Activate();
        }

        private void loadExample()
        {
            if (m_ExampleOSD == null)
            {
                const bool k_Clickthrough = true;

                m_ExampleOSD = new SimpleOsdForm(!k_Clickthrough);
                m_ExampleOSD.MouseUp += new MouseEventHandler(m_ExampleOSD_MouseUp);
                m_ExampleOSD.MouseDown += new MouseEventHandler(m_ExampleOSD_MouseDown);
                m_ExampleOSD.Move += new EventHandler(m_ExampleOSD_Move);
                m_ExampleOSD.Label.Text = "Example";
                updateExampleLocation();

                applySettingsToOSD(m_ExampleOSD);
            }

            m_ExampleOSD.ShowAlways(this);
        }

        private void applySettingsToOSD(SimpleOsdForm i_Osd)
        {
            i_Osd.HideFast();
            // apply initial settings
            i_Osd.Label.ForeColor = Properties.Settings.Default.ForeColor;
            i_Osd.Label.BackColor = Properties.Settings.Default.BackColor;
            i_Osd.Label.Font = Properties.Settings.Default.Font;
            i_Osd.Opacity = Properties.Settings.Default.Opacity;
            i_Osd.Border = Properties.Settings.Default.OsdBorder;
        }

        private bool m_OsdMove = false;

        void m_ExampleOSD_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                m_OsdMove = true;
            }
        }

        void m_ExampleOSD_Move(object sender, EventArgs e)
        {
            if (m_OsdMove)
            {
                tbLocation.Text = m_ExampleOSD.Location.ToString();
            }
        }

        void m_ExampleOSD_MouseUp(object sender, MouseEventArgs e)
        {
            if (m_OsdMove && e.Button == MouseButtons.Left)
            {
                m_OsdMove = false;
                tbLocation.Text = new PointConverter().ConvertToString(m_ExampleOSD.Location);
                Properties.Settings.Default.OsdPoint = (Point)(new PointConverter().ConvertFromString(tbLocation.Text));
                updateExampleLocation();
                updateLocation();
            }
        }

        private void updateExampleLocation()
        {
            const int k_PadTop = 10;
            Point location = Location;
            location.Y += Height + k_PadTop;
            m_ExampleOSD.Location = location;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            if (m_ExampleOSD != null)
            {
                updateExampleLocation();
            }
        }

        private void hideMyself()
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void checkBoxStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxStartWithWindows.Checked)
            {
                setOnStartup();
            }
            else
            {
                removeFromStartup();
            }
        }

        #region Registry Handling

        //HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
        const string k_RegSoftMicrosoftWinCurrVerRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string k_RegKeyboardIndicator = "KeyboardIndicator";
        const bool v_Writeable = true;

        private bool isOnStartup()
        {
            const bool v_OnStartup = true;
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(k_RegSoftMicrosoftWinCurrVerRun, !v_Writeable);
            object value = regKey.GetValue(k_RegKeyboardIndicator);

            return value == null ? !v_OnStartup : v_OnStartup;
        }

        private void setOnStartup()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(k_RegSoftMicrosoftWinCurrVerRun, v_Writeable);
            object value = regKey.GetValue(k_RegKeyboardIndicator);

            if (value == null)
            {
                string regStartup = Environment.CommandLine;
                regKey.SetValue(k_RegKeyboardIndicator, regStartup);
            }

            regKey.Close();
        }

        private void removeFromStartup()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(k_RegSoftMicrosoftWinCurrVerRun, v_Writeable);
            object value = regKey.GetValue(k_RegKeyboardIndicator);

            if (value != null)
            {
                regKey.DeleteValue(k_RegKeyboardIndicator);
            }

            regKey.Close();
        }

        #endregion Registry Handling

        private void loadSettings()
        {
            // Global config
            cbUseGlobalConfig.Checked = Properties.Settings.Default.UseGlobalConfig;

            // Registry
            checkBoxStartWithWindows.Checked = isOnStartup();
            
            // Sound
            m_EnableSound = Properties.Settings.Default.EnableSound;
            checkBoxEnableSound.Checked = m_EnableSound;
            
            if (m_EnableSound)
            {
                loadSound();
            }

            // Location
            try
            {
                m_OsdLocation = (eLocation)Enum.Parse(typeof(eLocation), Properties.Settings.Default.OsdLocation);
            }
            catch
            {
                m_OsdLocation = eLocation.BottomRight;
            }
            cbLocation.SelectedIndex = (int)m_OsdLocation;
            tbLocation.Text = new PointConverter().ConvertToString(Properties.Settings.Default.OsdPoint);

            // Text
            tbCapsLockOn.Text = Properties.Settings.Default.CapsLockOn.Trim();
            tbCapsLockOff.Text = Properties.Settings.Default.CapsLockOff.Trim();
            tbNumLockOn.Text = Properties.Settings.Default.NumLockOn.Trim();
            tbNumLockOff.Text = Properties.Settings.Default.NumLockOff.Trim();
            tbScrollLockOn.Text = Properties.Settings.Default.ScrollLockOn.Trim();
            tbScrollLockOff.Text = Properties.Settings.Default.ScrollLockOff.Trim();
            tbInsert.Text = Properties.Settings.Default.InsertPress.Trim();

            numOpacity.Value = (decimal) Properties.Settings.Default.Opacity;
            cbOsdBorder.Checked = Properties.Settings.Default.OsdBorder;

            // Animation
            try
            {
                m_HideAnimation = (AnimateWindowFlags)Enum.Parse(typeof(AnimateWindowFlags), Properties.Settings.Default.HideAnimation);
            }
            catch
            {
                m_HideAnimation = AnimateWindowFlags.AW_CENTER;
            }
            selectAnimationCb(m_HideAnimation);
            numAnimationSpeed.Value = Properties.Settings.Default.AnimationSpeed;
            
            // Interval
            numInterval.Value = Properties.Settings.Default.Interval;

            // Style
            try
            {
                m_Style = (eStyle)Enum.Parse(typeof(eStyle), Properties.Settings.Default.OsdStyle);
            }
            catch
            {
                m_Style = eStyle.Normal;
            }
            selectStyleCb(m_Style);

            updateModifiers();
            applySettings();
            updateStickyOSD();
        }

        private void loadSound()
        {
            if (m_SoundPlayer == null)
            {
                m_SoundPlayer = new SoundPlayer(Properties.Resources.WavClick);
                // This make sure first Play() execution wont freeze a little.
                m_SoundPlayer.LoadAsync();
                m_SoundPlayer.Stop();
            }
        }

        /// <summary>
        /// Apply settings from the example osd
        /// </summary>
        private void applySettings()
        {
            m_OSD.Animation = m_HideAnimation;
            m_OSD.IntervalAnimation = (int)numAnimationSpeed.Value;
            m_OSD.IntervalHide = (int)numInterval.Value;
            applySettingsToOSD(m_OSD);

            updateLocation();

            foreach (SimpleOsdForm o in m_ListStickyOSD)
                applySettingsToOSD(o);
        }

        private void saveSettings()
        {
            Properties.Settings.Default.CapsLockOn = tbCapsLockOn.Text.Trim();
            Properties.Settings.Default.CapsLockOff = tbCapsLockOff.Text.Trim();
            Properties.Settings.Default.NumLockOn = tbNumLockOn.Text.Trim();
            Properties.Settings.Default.NumLockOff = tbNumLockOff.Text.Trim();
            Properties.Settings.Default.ScrollLockOn = tbScrollLockOn.Text.Trim();
            Properties.Settings.Default.ScrollLockOff = tbScrollLockOff.Text.Trim();
            Properties.Settings.Default.InsertPress = tbInsert.Text.Trim();
            Properties.Settings.Default.OsdLocation = m_OsdLocation.ToString();
            Properties.Settings.Default.OsdPoint = (Point)(new PointConverter().ConvertFromString(tbLocation.Text));
            Properties.Settings.Default.OsdStyle = m_Style.ToString();
            Properties.Settings.Default.ForeColor = m_ExampleOSD.Label.ForeColor;
            Properties.Settings.Default.BackColor = m_ExampleOSD.Label.BackColor;
            Properties.Settings.Default.Font = m_ExampleOSD.Label.Font;
            Properties.Settings.Default.Opacity = m_ExampleOSD.Opacity;
            Properties.Settings.Default.HideAnimation = m_HideAnimation.ToString();
            Properties.Settings.Default.AnimationSpeed = (int)numAnimationSpeed.Value;
            Properties.Settings.Default.Interval = (int)numInterval.Value;
            Properties.Settings.Default.EnableSound = m_EnableSound;
            Properties.Settings.Default.UseGlobalConfig = cbUseGlobalConfig.Checked;
            Properties.Settings.Default.OsdBorder = cbOsdBorder.Checked;

            if (cbUseGlobalConfig.Checked)
            {
                saveSettingsGlobal();
            }
            else
            {
                Properties.Settings.Default.Save();
            }
        }

        private void saveSettingsGlobal()
        {
            CustomSettings.Default.Save();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            resetSettings();
        }

        private void resetSettings()
        {
            Properties.Settings.Default.Reset();
            loadSettings();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                hideMyself();
            }

            base.OnFormClosing(e);
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            showMyself();
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void checkBoxEnableSound_CheckedChanged(object sender, EventArgs e)
        {
            m_EnableSound = checkBoxEnableSound.Checked;

            if (m_EnableSound)
            {
                loadSound();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, r_AboutString, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonFont_Click(object sender, EventArgs e)
        {
            fontDialog.Font = m_ExampleOSD.Label.Font;

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                m_ExampleOSD.Label.Font = fontDialog.Font;
            }
        }

        private void cbLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_OsdLocation = (eLocation)cbLocation.SelectedIndex;
            tbLocation.Enabled = m_OsdLocation == eLocation.Manual;
            updateLocation();
        }

        private void numOpacity_ValueChanged(object sender, EventArgs e)
        {
            if (m_ExampleOSD != null)
            {
                m_ExampleOSD.Opacity = (double)numOpacity.Value;
            }
        }

        private void cbOsdBorder_CheckedChanged(object sender, EventArgs e)
        {
            if (m_ExampleOSD != null)
            {
                m_ExampleOSD.Border = cbOsdBorder.Checked;
            }
        }

        private void cbAnimation_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<string, AnimateWindowFlags> item = (KeyValuePair<string, AnimateWindowFlags>)cbAnimation.SelectedItem;
            m_HideAnimation = item.Value;
        }

        private void cbStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<string, eStyle> item = (KeyValuePair<string, eStyle>)cbStyle.SelectedItem;
            m_Style = item.Value;
            updateStickyOSD();
        }
    }
}
