using System.Drawing.Drawing2D;

namespace IndustrialPidLevelControlSimulator;

public partial class Form1 : Form
{
    private const int TankCapacityLiters = 1935;
    private const int GraphPoints = 100;
    private const double TwipsPerPixel = 15.0;
    private const int MenuOffsetY = 24;
    private static readonly Color PipeIdleColor = Color.FromArgb(120, 120, 120);
    private static readonly Color PipeWaterColor = Color.FromArgb(0, 255, 255);
    private static readonly Color PipeSupplyColor = Color.FromArgb(55, 150, 255);

    private readonly Random random = new();
    private readonly int[] outputGraph = new int[GraphPoints + 1];
    private readonly int[] processGraph = new int[GraphPoints + 1];
    private readonly int[] setpointGraph = new int[GraphPoints + 1];

    private MenuStrip mainMenu = null!;
    private ToolStripMenuItem fileMenu = null!;
    private ToolStripMenuItem exitMenuItem = null!;
    private ToolStripMenuItem instructionsMenuItem = null!;

    private Button manualButton = null!;
    private Button automaticButton = null!;
    private Button unstableButton = null!;

    private IndicatorLight manualLight = null!;
    private IndicatorLight autoLight = null!;
    private IndicatorLight unstableLight = null!;

    private HScrollBar inletScroll = null!;
    private HScrollBar outletScroll = null!;
    private VScrollBar setpointScroll = null!;

    private TextBox pvTextBox = null!;
    private TextBox kpTextBox = null!;
    private TextBox kiTextBox = null!;
    private TextBox kdTextBox = null!;

    private Label inletPercentLabel = null!;
    private Label outletPercentLabel = null!;
    private Label setpointValueLabel = null!;
    private Label errorValueLabel = null!;
    private Label inletValveStateLabel = null!;
    private Label outletValveStateLabel = null!;

    private PictureBox inletValvePicture = null!;
    private PictureBox outletValvePicture = null!;
    private PictureBox tankPicture = null!;

    private Panel topPipeHorizontal = null!;
    private Panel topPipeVertical = null!;
    private Panel bottomPipeVertical = null!;
    private Panel bottomPipeHorizontal = null!;
    private Panel sourcePipeHorizontal = null!;
    private Panel sourcePipeVertical = null!;

    private DoubleBufferedPanel setpointGaugePanel = null!;
    private DoubleBufferedPanel processChartPanel = null!;
    private DoubleBufferedPanel outputChartPanel = null!;
    private DoubleBufferedPanel loopDiagramPanel = null!;

    private System.Windows.Forms.Timer simulationTimer = null!;

    private Image inletClosedImage = null!;
    private Image inletOpenImage = null!;
    private Image outletOpenImage = null!;
    private Image outletClosedImage = null!;
    private Image tankImage = null!;

    // Original VB6 model state.
    private int inletValve;
    private int outletValve;
    private bool autoMode;
    private int error;
    private bool unstableSupply;
    private int instability;

    private int kp;
    private int ki;
    private int kd;
    private int output;
    private int processValue;
    private int s1;
    private int s2;

    private int derivativeInput;
    private int derivativeFiltered;
    private int lastInput;
    private int feedback;
    private int derivativeFilter;
    private int setPoint;
    private int previousInletPercent;
    private int previousOutletPercent;

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        LoadAssets();
        InitializeSimulation();
    }

    private void BuildUi()
    {
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);
        BackColor = Color.FromArgb(150, 150, 150);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(PxX(18600), PxY(7000) + MenuOffsetY);
        Text = "PID Simulation Example - Industrial Level Control";
        DoubleBuffered = true;

        mainMenu = new MenuStrip();
        mainMenu.Dock = DockStyle.Top;
        fileMenu = new ToolStripMenuItem("File");
        exitMenuItem = new ToolStripMenuItem("Exit");
        instructionsMenuItem = new ToolStripMenuItem("Instructions");

        exitMenuItem.Click += (_, _) => Close();
        instructionsMenuItem.Click += (_, _) => new InstructionsForm(this).ShowDialog(this);
        fileMenu.DropDownItems.Add(exitMenuItem);
        mainMenu.Items.Add(fileMenu);
        mainMenu.Items.Add(instructionsMenuItem);
        Controls.Add(mainMenu);

        inletValvePicture = new PictureBox
        {
            Location = UiPoint(555, 165),
            Size = UiSize(615, 510),
            BackColor = BackColor,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        Controls.Add(inletValvePicture);

        sourcePipeVertical = CreatePipe(new Point(0, mainMenu.Height), UiSize(120, 450));
        sourcePipeHorizontal = CreatePipe(UiPoint(0, 450), UiSize(585, 165));

        topPipeHorizontal = CreatePipe(UiPoint(1125, 465), UiSize(1275, 120));
        topPipeVertical = CreatePipe(UiPoint(2250, 570), UiSize(150, 465));
        bottomPipeVertical = CreatePipe(UiPoint(2250, 3630), UiSize(165, 270));
        bottomPipeHorizontal = CreatePipe(UiPoint(2220, 4440), UiSize(180, 405));

        Controls.Add(sourcePipeVertical);
        Controls.Add(sourcePipeHorizontal);
        Controls.Add(topPipeHorizontal);
        Controls.Add(topPipeVertical);
        Controls.Add(bottomPipeVertical);
        Controls.Add(bottomPipeHorizontal);

        tankPicture = new PictureBox
        {
            Location = UiPoint(1590, 975),
            Size = UiSize(1455, 2700),
            BackColor = BackColor,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        tankPicture.Paint += TankPicture_Paint;
        Controls.Add(tankPicture);

        outletValvePicture = new PictureBox
        {
            Location = UiPoint(2175, 3855),
            Size = UiSize(510, 615),
            BackColor = BackColor,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        Controls.Add(outletValvePicture);

        inletScroll = new HScrollBar
        {
            Location = UiPoint(90, 930),
            Size = UiSize(1425, 255),
            Minimum = 0,
            Maximum = 100 + 9,
            SmallChange = 1,
            LargeChange = 10
        };
        inletScroll.ValueChanged += (_, _) => InletScrollChanged();
        Controls.Add(inletScroll);

        outletScroll = new HScrollBar
        {
            Location = UiPoint(285, 4680),
            Size = UiSize(1365, 255),
            Minimum = 0,
            Maximum = 100 + 9,
            SmallChange = 1,
            LargeChange = 10
        };
        outletScroll.ValueChanged += (_, _) => OutletScrollChanged();
        Controls.Add(outletScroll);

        setpointGaugePanel = new DoubleBufferedPanel
        {
            Location = UiPoint(3060, 1395),
            Size = UiSize(195, 1935),
            BackColor = Color.Black
        };
        setpointGaugePanel.Paint += SetpointGaugePanel_Paint;
        Controls.Add(setpointGaugePanel);

        setpointScroll = new VScrollBar
        {
            Location = UiPoint(3270, 1155),
            Size = UiSize(255, 2445),
            Minimum = 0,
            Maximum = TankCapacityLiters + 9,
            SmallChange = 1,
            LargeChange = 10
        };
        setpointScroll.ValueChanged += (_, _) => SetpointScrollChanged();
        Controls.Add(setpointScroll);

        manualButton = new Button
        {
            Text = "Manual Control",
            Location = UiPoint(4875, 180),
            Size = UiSize(1470, 315)
        };
        manualButton.Click += (_, _) => SetAutoMode(false);
        Controls.Add(manualButton);

        automaticButton = new Button
        {
            Text = "Automatic Control",
            Location = UiPoint(4875, 540),
            Size = UiSize(1470, 315)
        };
        automaticButton.Click += (_, _) => SetAutoMode(true);
        Controls.Add(automaticButton);

        manualLight = new IndicatorLight { Location = UiPoint(6390, 210), Size = UiSize(255, 285) };
        autoLight = new IndicatorLight { Location = UiPoint(6390, 570), Size = UiSize(255, 285) };
        unstableLight = new IndicatorLight { Location = UiPoint(6570, 4065), Size = UiSize(255, 285) };

        Controls.Add(manualLight);
        Controls.Add(autoLight);
        Controls.Add(unstableLight);

        kpTextBox = CreatePidBox(UiPoint(6000, 990), UiSize(615, 315), "30");
        kiTextBox = CreatePidBox(UiPoint(6000, 1410), UiSize(645, 285), "3");
        kdTextBox = CreatePidBox(UiPoint(6000, 1800), UiSize(645, 285), "10");
        Controls.Add(kpTextBox);
        Controls.Add(kiTextBox);
        Controls.Add(kdTextBox);

        pvTextBox = new TextBox
        {
            Location = UiPoint(660, 2670),
            Size = UiSize(675, 285),
            Font = new Font("Arial", 11.25f, FontStyle.Bold),
            ReadOnly = true,
            TextAlign = HorizontalAlignment.Center,
            BorderStyle = BorderStyle.None,
            Text = "0"
        };
        Controls.Add(pvTextBox);

        processChartPanel = new DoubleBufferedPanel
        {
            Location = UiPoint(11430, 405),
            Size = UiSize(6500, 2145),
            BackColor = Color.Black
        };
        processChartPanel.Paint += ProcessChartPanel_Paint;
        Controls.Add(processChartPanel);

        outputChartPanel = new DoubleBufferedPanel
        {
            Location = UiPoint(11430, 2970),
            Size = UiSize(6500, 2145),
            BackColor = Color.Black
        };
        outputChartPanel.Paint += OutputChartPanel_Paint;
        Controls.Add(outputChartPanel);

        loopDiagramPanel = new DoubleBufferedPanel
        {
            Location = UiPoint(6000, 2550),
            Size = UiSize(5000, 1900),
            BackColor = BackColor
        };
        loopDiagramPanel.Paint += LoopDiagramPanel_Paint;
        Controls.Add(loopDiagramPanel);

        unstableButton = new Button
        {
            Text = "Create Instability\r\nby Adding Water",
            Location = UiPoint(525, 5550),
            Size = UiSize(1275, 660)
        };
        unstableButton.Click += (_, _) => ToggleInstability();
        Controls.Add(unstableButton);

        AddStaticLabels();

        inletPercentLabel = CreateValueLabel(UiPoint(570, 645), UiSize(780, 285), "100", Color.Black, 11f);
        outletPercentLabel = CreateValueLabel(UiPoint(2670, 4080), UiSize(780, 285), "0", Color.Black, 11f);
        setpointValueLabel = CreateValueLabel(UiPoint(4200, 3960), UiSize(850, 320), "0", Color.Black, 13f);
        errorValueLabel = CreateValueLabel(UiPoint(6030, 2220), UiSize(585, 255), "0", Color.Black, 14f);
        inletValveStateLabel = CreateStatusLabel(UiPoint(120, 1740), UiSize(1950, 255));
        outletValveStateLabel = CreateStatusLabel(UiPoint(90, 5430), UiSize(3100, 255));

        Controls.Add(inletPercentLabel);
        Controls.Add(outletPercentLabel);
        Controls.Add(setpointValueLabel);
        Controls.Add(errorValueLabel);
        Controls.Add(inletValveStateLabel);
        Controls.Add(outletValveStateLabel);

        simulationTimer = new System.Windows.Forms.Timer { Interval = 100, Enabled = true };
        simulationTimer.Tick += (_, _) => TimerTick();

        MainMenuStrip = mainMenu;
        ResumeLayout(false);
        PerformLayout();
    }

    private void AddStaticLabels()
    {
        Controls.Add(CreateCaption(
            "In this example, actual time is not shown in seconds or minutes.\r\nTime is scaled so the control loop can be visualized.",
            UiPoint(6300, 6210),
            UiSize(6200, 600),
            Color.Teal));

        Controls.Add(CreateCaption("Manual Valve", UiPoint(1380, 150), UiSize(1065, 225)));
        Controls.Add(CreateCaption("Valve", UiPoint(990, 4350), UiSize(630, 255)));
        Controls.Add(CreateCaption("Position 0-100%", UiPoint(210, 1200), UiSize(1185, 255)));
        Controls.Add(CreateCaption("Position 0-100%", UiPoint(375, 4950), UiSize(1215, 225)));
        Controls.Add(CreateCaption("200 Liters/Min Max.", UiPoint(75, 1440), UiSize(1425, 255), Color.Blue));
        Controls.Add(CreateCaption("3000 Liters/Min Max.", UiPoint(105, 5160), UiSize(1575, 255), Color.Blue));

        Controls.Add(CreateCaption("Tank of 1935 Liters", UiPoint(75, 2190), UiSize(1440, 255)));
        Controls.Add(CreateCaption("PV", UiPoint(210, 2640), UiSize(660, 345), Color.Red, 18f, FontStyle.Bold));

        Controls.Add(CreateCaption("Setpoint Level", UiPoint(3630, 390), UiSize(1700, 225)));
        Controls.Add(CreateCaption("SP", UiPoint(3950, 3340), UiSize(700, 345), Color.Red, 18f, FontStyle.Bold));
        Controls.Add(CreateCaption("Liters", UiPoint(4170, 3690), UiSize(950, 225), Color.Black, 10f, FontStyle.Bold));

        Controls.Add(CreateCaption("OUT", UiPoint(2460, 4515), UiSize(900, 375), Color.Red, 18f, FontStyle.Bold));
        Controls.Add(CreateCaption("% Valve Position", UiPoint(2450, 4815), UiSize(1500, 255)));

        Controls.Add(CreateCaption("Proportional (Kp)", UiPoint(4560, 1050), UiSize(1395, 255)));
        Controls.Add(CreateCaption("Integral (Ki)", UiPoint(4740, 1440), UiSize(1245, 285)));
        Controls.Add(CreateCaption("Derivative (Kd)", UiPoint(4680, 1830), UiSize(1305, 255)));
        Controls.Add(CreateCaption("0-100%", UiPoint(6660, 1080), UiSize(585, 225)));
        Controls.Add(CreateCaption("0-120 Seconds", UiPoint(6660, 1470), UiSize(1095, 225)));
        Controls.Add(CreateCaption("0-120 Seconds", UiPoint(6660, 1860), UiSize(1110, 225)));

        Controls.Add(CreateCaption("e = error (difference)", UiPoint(4560, 2220), UiSize(1425, 225)));

        Controls.Add(CreateCaption("Process Variable - PV", UiPoint(13230, 150), UiSize(2300, 255), Color.Black, 10f, FontStyle.Bold));
        Controls.Add(CreateCaption("Valve Position Output", UiPoint(13130, 2730), UiSize(2600, 225), Color.Black, 10f, FontStyle.Bold));
        Controls.Add(CreateCaption("2500 Liters", UiPoint(11160, 420), UiSize(1000, 210)));
        Controls.Add(CreateCaption("0 Liters", UiPoint(11220, 2355), UiSize(780, 210)));
        Controls.Add(CreateCaption("100%", UiPoint(11220, 2940), UiSize(620, 285)));
        Controls.Add(CreateCaption("0%", UiPoint(11280, 4890), UiSize(500, 210)));
        Controls.Add(CreateCaption("T", UiPoint(17730, 2550), UiSize(260, 285)));
        Controls.Add(CreateCaption("T", UiPoint(17700, 5115), UiSize(260, 285)));
    }

    private static int PxX(int twips) => (int)Math.Round(twips / TwipsPerPixel);

    private static int PxY(int twips) => (int)Math.Round(twips / TwipsPerPixel);

    private static Point UiPoint(int leftTwips, int topTwips) => new(PxX(leftTwips), PxY(topTwips) + MenuOffsetY);

    private static Size UiSize(int widthTwips, int heightTwips) => new(PxX(widthTwips), PxY(heightTwips));

    private static Panel CreatePipe(Point location, Size size)
    {
        return new Panel
        {
            Location = location,
            Size = size,
            BackColor = PipeIdleColor,
            BorderStyle = BorderStyle.None
        };
    }

    private static TextBox CreatePidBox(Point location, Size size, string text)
    {
        return new TextBox
        {
            Location = location,
            Size = size,
            Text = text,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Arial", 9.75f, FontStyle.Bold)
        };
    }

    private static Label CreateValueLabel(Point location, Size size, string text, Color color, float fontSize)
    {
        return new Label
        {
            Location = location,
            AutoSize = false,
            Size = size,
            Text = text,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = new Font("Arial", fontSize, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static Label CreateStatusLabel(Point location, Size size)
    {
        return new Label
        {
            Location = location,
            AutoSize = false,
            Size = size,
            Text = string.Empty,
            ForeColor = Color.Navy,
            BackColor = Color.Transparent,
            Font = new Font("Arial", 8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Label CreateCaption(string text, Point location, Size size, Color? color = null, float fontSize = 9f, FontStyle style = FontStyle.Regular)
    {
        return new Label
        {
            Location = location,
            AutoSize = false,
            Size = size,
            Text = text,
            ForeColor = color ?? Color.Black,
            BackColor = Color.Transparent,
            Font = new Font("Arial", fontSize, style)
        };
    }

    private void LoadAssets()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        inletOpenImage = Image.FromFile(Path.Combine(basePath, "Assets", "Dibujo1.bmp"));
        inletClosedImage = Image.FromFile(Path.Combine(basePath, "Assets", "Dibujo6.bmp"));
        outletOpenImage = Image.FromFile(Path.Combine(basePath, "Assets", "Dibujo2.bmp"));
        outletClosedImage = Image.FromFile(Path.Combine(basePath, "Assets", "Dibujo4.bmp"));
        tankImage = Image.FromFile(Path.Combine(basePath, "Assets", "Dibujo3.bmp"));

        tankPicture.Image = tankImage;
    }

    private void InitializeSimulation()
    {
        instability = 2000;

        inletScroll.Value = 100;
        inletPercentLabel.Text = inletScroll.Value.ToString();
        inletValve = VbLong((inletScroll.Value * (instability / 100.0)) / 60.0);

        outletScroll.Value = 0;
        outletPercentLabel.Text = "0";
        outletValve = 0;

        kp = ClampTextBox(kpTextBox, 0, 100);
        ki = ClampTextBox(kiTextBox, 0, 120);
        kd = ClampTextBox(kdTextBox, 0, 120);

        setpointScroll.Value = ToScrollValue(1167);
        setPoint = 1167;
        setpointValueLabel.Text = setPoint.ToString();

        processValue = 0;
        pvTextBox.Text = processValue.ToString();

        previousInletPercent = inletScroll.Value;
        previousOutletPercent = outletScroll.Value;
        SetAutoMode(true);
        UpdateValveImages();
        UpdateTankPipes();
        UpdateValveStatusLabels(false);

        processChartPanel.Invalidate();
        outputChartPanel.Invalidate();
    }

    private void InletScrollChanged()
    {
        inletPercentLabel.Text = inletScroll.Value.ToString();
        inletValve = VbLong((inletScroll.Value * (instability / 100.0)) / 60.0);
        inletValvePicture.Image = inletScroll.Value == 0 ? inletClosedImage : inletOpenImage;
        UpdateValveStatusLabels();
        UpdateTankPipes();
    }

    private void OutletScrollChanged()
    {
        outletPercentLabel.Text = outletScroll.Value.ToString();
        outletValve = VbLong((outletScroll.Value * 30.0) / 60.0);
        UpdateValveStatusLabels();
        UpdateValveImages();
        UpdateTankPipes();
    }

    private void SetpointScrollChanged()
    {
        setPoint = FromScrollValue(setpointScroll.Value);
        setpointValueLabel.Text = setPoint.ToString();
        setpointGaugePanel.Invalidate();
    }

    private void SetAutoMode(bool enabled)
    {
        autoMode = enabled;
        outletScroll.Enabled = !enabled;

        manualLight.IsOn = !enabled;
        autoLight.IsOn = enabled;
    }

    private void ToggleInstability()
    {
        if (!unstableSupply)
        {
            unstableSupply = true;
            unstableLight.IsOn = true;
            return;
        }

        unstableSupply = false;
        unstableLight.IsOn = false;
        instability = 2000;
    }

    private void TimerTick()
    {
        kp = ClampTextBox(kpTextBox, 0, 100);
        ki = ClampTextBox(kiTextBox, 0, 120);
        kd = ClampTextBox(kdTextBox, 0, 120);

        if (processValue < 1936)
        {
            processValue += inletValve;
        }

        if (processValue > 0)
        {
            processValue -= outletValve;
        }

        pvTextBox.Text = processValue.ToString();
        error = setPoint - processValue;
        errorValueLabel.Text = error.ToString();

        UpdateTankPipes();
        tankPicture.Invalidate();

        if (unstableSupply)
        {
            WaterInstability();
            inletValve = VbLong((inletScroll.Value * (instability / 100.0)) / 60.0);
        }

        if (autoMode)
        {
            PidLoop();
        }

        UpdateValveStatusLabels();

        processGraph[GraphPoints] = processValue;
        setpointGraph[GraphPoints] = setPoint;
        outputGraph[GraphPoints] = outletValve;

        for (var i = 0; i < GraphPoints; i++)
        {
            processGraph[i] = processGraph[i + 1];
            setpointGraph[i] = setpointGraph[i + 1];
            outputGraph[i] = outputGraph[i + 1];
        }

        processChartPanel.Invalidate();
        outputChartPanel.Invalidate();
    }

    private void WaterInstability()
    {
        s1 = random.Next(1, 21);
        s2 = random.Next(1, 1001);

        if (s2 < 100)
        {
            instability += s1;
        }

        if (s2 > 900)
        {
            instability -= s1;
        }

        if (instability < 500)
        {
            instability = 500;
        }

        if (instability > 2500)
        {
            instability = 2500;
        }
    }

    private void PidLoop()
    {
        derivativeFilter = 10;
        derivativeInput = VbLong(processValue + (lastInput - processValue) * (kd / 60.0));
        lastInput = processValue;

        derivativeFiltered = VbLong(derivativeFiltered + (derivativeInput - derivativeFiltered) * derivativeFilter / 60.0);
        output = VbLong((setPoint - derivativeFiltered) * (kp / 100.0) + feedback);

        if (output > 100)
        {
            output = 100;
        }

        if (output < 0)
        {
            output = 0;
        }

        outletScroll.Value = Math.Clamp(100 - output, outletScroll.Minimum, outletScroll.Maximum - outletScroll.LargeChange + 1);
        outletPercentLabel.Text = outletScroll.Value.ToString();

        feedback = VbLong(feedback - (feedback - output) * ki / 60.0);
    }

    private void UpdateValveImages()
    {
        outletValvePicture.Image = outletScroll.Value > 0 && processValue > 0 ? outletOpenImage : outletClosedImage;
    }

    private void UpdateValveStatusLabels(bool trackDelta = true)
    {
        inletValveStateLabel.Text = $"Inlet: {GetValveStatus(inletScroll.Value, previousInletPercent, true)}";
        outletValveStateLabel.Text = $"Outlet: {GetValveStatus(outletScroll.Value, previousOutletPercent, false)}";
        inletValveStateLabel.ForeColor = inletScroll.Value == 0
            ? Color.Maroon
            : inletScroll.Value >= 100
                ? Color.DarkGreen
                : Color.Navy;
        outletValveStateLabel.ForeColor = outletScroll.Value == 0
            ? Color.Maroon
            : outletScroll.Value >= 100
                ? Color.DarkGreen
                : Color.Navy;

        if (trackDelta)
        {
            previousInletPercent = inletScroll.Value;
            previousOutletPercent = outletScroll.Value;
        }
    }

    private string GetValveStatus(int percent, int previousPercent, bool isInlet)
    {
        string state;
        if (percent <= 0)
        {
            state = "Closed";
        }
        else if (percent >= 100)
        {
            state = "Fully Open";
        }
        else if (percent > previousPercent)
        {
            state = "Opening";
        }
        else if (percent < previousPercent)
        {
            state = "Closing";
        }
        else
        {
            state = "Open";
        }

        if (!isInlet && percent > 0 && processValue <= 0)
        {
            state = $"{state} (No Flow)";
        }

        return $"{state} - {percent}%";
    }

    private void UpdateTankPipes()
    {
        var inletOn = inletScroll.Value > 0;
        sourcePipeVertical.BackColor = PipeSupplyColor;
        sourcePipeHorizontal.BackColor = PipeSupplyColor;
        topPipeHorizontal.BackColor = inletOn ? PipeWaterColor : PipeIdleColor;
        topPipeVertical.BackColor = inletOn ? PipeWaterColor : PipeIdleColor;

        var outletOn = outletScroll.Value > 0 && processValue > 0;
        bottomPipeVertical.BackColor = outletOn ? PipeWaterColor : PipeIdleColor;
        bottomPipeHorizontal.BackColor = outletOn ? PipeWaterColor : PipeIdleColor;

        UpdateValveImages();
    }

    private void TankPicture_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var fillRatio = Math.Clamp(processValue, 0, TankCapacityLiters) / (double)TankCapacityLiters;

        // Black framed viewing window into the tank.
        var windowX = (int)Math.Round(tankPicture.ClientSize.Width * (24.0 / 97.0));
        var windowY = (int)Math.Round(tankPicture.ClientSize.Height * (28.0 / 180.0));
        var windowW = (int)Math.Round(tankPicture.ClientSize.Width * (48.0 / 97.0));
        var windowH = (int)Math.Round(tankPicture.ClientSize.Height * (129.0 / 180.0));
        var windowRect = new Rectangle(windowX, windowY, windowW, windowH);

        using var darkBrush = new SolidBrush(Color.FromArgb(220, 8, 12, 18));
        e.Graphics.FillRectangle(darkBrush, windowRect);

        var bodyFillHeight = (int)Math.Round(windowH * fillRatio);
        var bodyBottom = windowY + windowH;
        var bodyTop = bodyBottom - bodyFillHeight;

        if (bodyFillHeight > 0)
        {
            using var waterPath = new GraphicsPath();
            var waveAmplitude = Math.Max(1f, windowH / 65f);
            var phase = (float)(Environment.TickCount / 180.0);
            const int waveSegments = 30;
            var wavePoints = new List<PointF>();

            for (var i = 0; i <= waveSegments; i++)
            {
                var x = windowX + (windowW * i / (float)waveSegments);
                var theta = (i / (float)waveSegments) * MathF.PI * 2f + phase;
                var y = bodyTop + MathF.Sin(theta) * waveAmplitude;
                wavePoints.Add(new PointF(x, y));
            }

            var polygonPoints = new List<PointF>(wavePoints.Count + 2)
            {
                new(windowX, bodyBottom),
                new(windowX + windowW, bodyBottom)
            };

            for (var i = wavePoints.Count - 1; i >= 0; i--)
            {
                polygonPoints.Add(wavePoints[i]);
            }

            waterPath.AddPolygon(polygonPoints.ToArray());

            using var bodyBrush = new LinearGradientBrush(
                new Point(windowX, bodyTop),
                new Point(windowX, bodyBottom),
                Color.FromArgb(140, 20, 245, 255),
                Color.FromArgb(200, 0, 130, 255));
            e.Graphics.FillPath(bodyBrush, waterPath);

            using var wavePen = new Pen(Color.FromArgb(235, 200, 255, 255), 1.4f);
            e.Graphics.DrawLines(wavePen, wavePoints.ToArray());
        }

        using var framePen = new Pen(Color.Black, 2f);
        e.Graphics.DrawRectangle(framePen, windowRect);
    }

    private void SetpointGaugePanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.Clear(Color.Black);

        var inner = new Rectangle(2, 2, setpointGaugePanel.Width - 5, setpointGaugePanel.Height - 5);
        using var borderPen = new Pen(Color.Red, 2);
        e.Graphics.DrawRectangle(borderPen, inner);

        var fillHeight = (int)Math.Round((inner.Height - 2) * (setPoint / (double)TankCapacityLiters));
        var fillRect = new Rectangle(inner.Left + 1, inner.Bottom - 1 - fillHeight, inner.Width - 2, fillHeight);
        using var fillBrush = new SolidBrush(Color.Yellow);
        e.Graphics.FillRectangle(fillBrush, fillRect);
    }

    private void ProcessChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.Clear(Color.Black);

        for (var i = 0; i < GraphPoints; i++)
        {
            var x = i * (processChartPanel.Width - 1) / (GraphPoints - 1);

            var pvY = ScaleY(2500 - processGraph[i], 0, 2500, processChartPanel.Height);
            var spY = ScaleY(2500 - setpointGraph[i], 0, 2500, processChartPanel.Height);

            DrawPoint(e.Graphics, Color.Cyan, x, pvY);
            DrawPoint(e.Graphics, Color.Yellow, x, spY);
        }
    }

    private void OutputChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.Clear(Color.Black);

        for (var i = 0; i < GraphPoints; i++)
        {
            var x = i * (outputChartPanel.Width - 1) / (GraphPoints - 1);
            var y = ScaleY(100 - (outputGraph[i] * 2), 0, 105, outputChartPanel.Height);
            DrawPoint(e.Graphics, Color.Red, x, y);
        }
    }

    private void LoopDiagramPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        using var normalFont = new Font("Arial", 8f, FontStyle.Regular);
        using var boldFont = new Font("Arial", 8f, FontStyle.Bold);
        using var linePen = new Pen(Color.Black, 1.2f);
        using var accentPen = new Pen(Color.DarkSlateBlue, 1.3f);

        using var panelBackBrush = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(panelBackBrush, new Rectangle(0, 0, loopDiagramPanel.Width, loopDiagramPanel.Height));

        var margin = 16f;
        var centerY = 50f;
        var sumCenter = new PointF(margin + 14, centerY);
        var sumRadius = 13f;
        var pidRect = new RectangleF(sumCenter.X + 25, centerY - 15, 88, 30);
        var valveRect = new RectangleF(pidRect.Right + 20, centerY - 15, 64, 30);
        var processRect = new RectangleF(valveRect.Right + 20, centerY - 15, 96, 30);
        var outX = processRect.Right + 18;

        e.Graphics.DrawEllipse(accentPen, sumCenter.X - sumRadius, sumCenter.Y - sumRadius, sumRadius * 2, sumRadius * 2);
        e.Graphics.DrawString("Σ", boldFont, Brushes.DarkSlateBlue, sumCenter.X - 4, sumCenter.Y - 5);

        e.Graphics.FillRectangle(Brushes.White, Rectangle.Round(pidRect));
        e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(pidRect));
        e.Graphics.DrawString("PID Controller", boldFont, Brushes.Black, pidRect.X + 8, pidRect.Y + 2);
        e.Graphics.DrawString($"Kp:{kp}  Ki:{ki}  Kd:{kd}", normalFont, Brushes.Black, pidRect.X + 6, pidRect.Y + 15);

        e.Graphics.FillRectangle(Brushes.White, Rectangle.Round(valveRect));
        e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(valveRect));
        e.Graphics.DrawString("Valve", boldFont, Brushes.Black, valveRect.X + 14, valveRect.Y + 8);

        e.Graphics.FillRectangle(Brushes.White, Rectangle.Round(processRect));
        e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(processRect));
        e.Graphics.DrawString("Tank Process", boldFont, Brushes.Black, processRect.X + 12, processRect.Y + 8);

        DrawArrow(e.Graphics, linePen, new PointF(6, sumCenter.Y), new PointF(sumCenter.X - sumRadius, sumCenter.Y));
        DrawArrow(e.Graphics, linePen, new PointF(sumCenter.X + sumRadius, sumCenter.Y), new PointF(pidRect.X, sumCenter.Y));
        DrawArrow(e.Graphics, linePen, new PointF(pidRect.Right, sumCenter.Y), new PointF(valveRect.X, sumCenter.Y));
        DrawArrow(e.Graphics, linePen, new PointF(valveRect.Right, sumCenter.Y), new PointF(processRect.X, sumCenter.Y));
        DrawArrow(e.Graphics, linePen, new PointF(processRect.Right, sumCenter.Y), new PointF(outX, sumCenter.Y));

        var fbDownY = 96f;
        e.Graphics.DrawLine(linePen, processRect.X + processRect.Width / 2f, processRect.Bottom, processRect.X + processRect.Width / 2f, fbDownY);
        DrawArrow(e.Graphics, linePen, new PointF(processRect.X + processRect.Width / 2f, fbDownY), new PointF(sumCenter.X, fbDownY));
        DrawArrow(e.Graphics, linePen, new PointF(sumCenter.X, fbDownY), new PointF(sumCenter.X, sumCenter.Y + sumRadius));

        e.Graphics.DrawString("SP", boldFont, Brushes.Black, 2, centerY - 12);
        e.Graphics.DrawString("e", boldFont, Brushes.Black, sumCenter.X + 5, centerY - 22);
        e.Graphics.DrawString("OUT", boldFont, Brushes.Black, outX + 2, centerY - 12);
        e.Graphics.DrawString("PV feedback", normalFont, Brushes.Black, valveRect.X + 50, fbDownY - 14);
        e.Graphics.DrawString($"SP:{setPoint} L   PV:{processValue} L   OUT:{outletScroll.Value}%   Error:{error}", normalFont, Brushes.Black, 8, 8);
    }

    private static int ScaleY(int value, int min, int max, int height)
    {
        var clamped = Math.Clamp(value, min, max);
        var normalized = (clamped - min) / (double)(max - min);
        return (int)Math.Round((1 - normalized) * (height - 1));
    }

    private static void DrawPoint(Graphics graphics, Color color, int x, int y)
    {
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, x, y, 2, 2);
    }

    private static void DrawArrow(Graphics graphics, Pen pen, PointF start, PointF end)
    {
        graphics.DrawLine(pen, start, end);

        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        const float headLength = 4f;
        const float headAngle = 0.45f;

        var p1 = new PointF(
            end.X - headLength * (float)Math.Cos(angle - headAngle),
            end.Y - headLength * (float)Math.Sin(angle - headAngle));
        var p2 = new PointF(
            end.X - headLength * (float)Math.Cos(angle + headAngle),
            end.Y - headLength * (float)Math.Sin(angle + headAngle));

        graphics.DrawLine(pen, end, p1);
        graphics.DrawLine(pen, end, p2);
    }

    private static int VbLong(double value)
    {
        return (int)Math.Round(value, MidpointRounding.ToEven);
    }

    private static int ClampTextBox(TextBox box, int min, int max)
    {
        if (!double.TryParse(box.Text, out var parsed))
        {
            parsed = min;
        }

        var value = (int)Math.Clamp(VbLong(parsed), min, max);

        if (box.Text != value.ToString())
        {
            box.Text = value.ToString();
        }

        return value;
    }

    private int ToScrollValue(int setpoint)
    {
        var maxValue = setpointScroll.Maximum - setpointScroll.LargeChange + 1;
        return Math.Clamp(TankCapacityLiters - setpoint, setpointScroll.Minimum, maxValue);
    }

    private int FromScrollValue(int scrollValue)
    {
        return Math.Clamp(TankCapacityLiters - scrollValue, 0, TankCapacityLiters);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        simulationTimer.Stop();

        inletOpenImage?.Dispose();
        inletClosedImage?.Dispose();
        outletOpenImage?.Dispose();
        outletClosedImage?.Dispose();
        tankImage?.Dispose();

        base.OnFormClosing(e);
    }
}

internal sealed class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}

internal sealed class IndicatorLight : Control
{
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool IsOn
    {
        get => isOn;
        set
        {
            if (isOn == value)
            {
                return;
            }

            isOn = value;
            Invalidate();
        }
    }

    private bool isOn;

    public IndicatorLight()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true);
        Size = new Size(18, 18);
        BackColor = SystemColors.Control;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var brush = new SolidBrush(IsOn ? Color.Lime : Color.FromArgb(150, 150, 150));
        using var pen = new Pen(Color.Black, 1.2f);

        e.Graphics.FillEllipse(brush, rect);
        e.Graphics.DrawEllipse(pen, rect);
    }
}

