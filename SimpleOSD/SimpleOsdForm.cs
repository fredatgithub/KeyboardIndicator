using System;
using System.Windows.Forms;
using System.Drawing;

namespace SimpleOSD
{
  public class SimpleOsdForm
  {
    private const int k_DefaultIntervalHide = 1000;
    private const int k_DefaultAnimationSpeed = 100;
    private const WinApi.AnimateWindowFlags k_DefaultAnimation = WinApi.AnimateWindowFlags.AW_CENTER;

    private OsdForm m_OsdForm = new OsdForm();
    private Timer m_Timer = new Timer();

    public event EventHandler Move
    {
      add { m_OsdForm.Move += value; }
      remove { m_OsdForm.Move -= value; }
    }

    public event MouseEventHandler MouseDown
    {
      add { m_OsdForm.MouseDown += value; }
      remove { m_OsdForm.MouseDown -= value; }
    }

    public event MouseEventHandler MouseUp
    {
      add { m_OsdForm.MouseUp += value; }
      remove { m_OsdForm.MouseUp -= value; }
    }

    public SimpleOsdForm()
    {
      IntervalHide = k_DefaultIntervalHide;
      IntervalAnimation = k_DefaultAnimationSpeed;

      m_Timer.Enabled = false;
      m_Timer.Tick += new EventHandler(m_Timer_Tick);

      Opacity = 1;
    }

    public SimpleOsdForm(bool i_Clickthrough)
        : this()
    {
      m_OsdForm.Clickhrough = i_Clickthrough;
    }

    private void m_Timer_Tick(object sender, EventArgs e)
    {
      Hide();
    }

    public int IntervalHide
    {
      get { return m_Timer.Interval; }
      set { m_Timer.Interval = value; }
    }

    private WinApi.AnimateWindowFlags m_Animation = k_DefaultAnimation;

    public WinApi.AnimateWindowFlags Animation
    {
      get { return m_Animation; }
      set
      {
        // workaround opacity bug when using blend animation
        // create new osd object when changing from blend
        if (m_Animation != value)
        {
          if (m_Animation == WinApi.AnimateWindowFlags.AW_BLEND)
          {
            // change from blend
            bool clickthrough = m_OsdForm.Clickhrough;
            OsdForm o = new OsdForm(m_OsdForm.Clickhrough);
            m_OsdForm.Close();
            m_OsdForm = o;
          }
          else if (value == WinApi.AnimateWindowFlags.AW_BLEND)
          {
            // workaround first blend animation not working
            m_OsdForm.Opacity = 0;
            m_OsdForm.Show();
            m_OsdForm.HideAnimate(1, value);
            m_OsdForm.WindowState = FormWindowState.Normal;
          }

          m_Animation = value;
        }
        m_Animation = value;
      }
    }

    public int IntervalAnimation { get; set; }

    public Label Label
    {
      get { return m_OsdForm.Label; }
    }

    public void Show()
    {
      m_Timer.Stop();
      m_OsdForm.ShowAnimate();
      m_Timer.Start();
    }

    public void ShowAlways()
    {
      m_Timer.Stop();
      m_OsdForm.Show();
    }

    public void ShowAlways(IWin32Window i_Owner)
    {
      m_Timer.Stop();
      m_OsdForm.Hide();
      m_OsdForm.Show(i_Owner);
    }

    public void Hide()
    {
      m_Timer.Stop();
      m_OsdForm.HideAnimate(IntervalAnimation, Animation);
    }

    public void HideFast()
    {
      m_OsdForm.Hide();
    }

    public void Close()
    {
      m_OsdForm.Close();
    }

    public Point Location
    {
      get { return m_OsdForm.Location; }
      set { m_OsdForm.Location = value; }
    }

    public void CenterToScreen()
    {
      m_OsdForm.CenterToScreen();
    }

    public int Height
    {
      get { return m_OsdForm.Height; }
    }

    public int Width
    {
      get { return m_OsdForm.Width; }
    }

    public bool Visible
    {
      get { return m_OsdForm.Visible; }
    }

    public double Opacity
    {
      get { return m_OsdForm.Opacity; }
      set
      {
        // workaround for blend animation
        if (value != m_OsdForm.Opacity && m_Animation != WinApi.AnimateWindowFlags.AW_BLEND)
        {
          if ((int)value == 1)    // workaround opacity 100%
            m_OsdForm.Opacity = 0.99;
          m_OsdForm.Opacity = value;
        }
      }
    }

    public bool Border
    {
      get { return m_OsdForm.Label.BorderStyle != BorderStyle.None; }
      set
      {
        if (value)
          m_OsdForm.Label.BorderStyle = BorderStyle.FixedSingle;
        else
          m_OsdForm.Label.BorderStyle = BorderStyle.None;
      }
    }
  }
}
