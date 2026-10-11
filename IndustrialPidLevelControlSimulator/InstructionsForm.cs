namespace IndustrialPidLevelControlSimulator;

internal sealed class InstructionsForm : Form
{
    public InstructionsForm(Form owner)
    {
        AutoScaleMode = AutoScaleMode.None;
        Text = "Simple PID Loop Explanation and Instructions";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(930, 720);

        var titleLabel = new Label
        {
            Text = "Explanation of Proportional, Integral, Derivative (PID) control",
            Location = new Point(18, 10),
            Size = new Size(885, 35),
            Font = new Font("Segoe UI", 17f, FontStyle.Bold)
        };

        var contentBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            Location = new Point(24, 55),
            Size = new Size(885, 650),
            BackColor = BackColor,
            Text = BuildInstructionsText()
        };

        Controls.Add(titleLabel);
        Controls.Add(contentBox);

        Left = owner.Left + (owner.Width - Width) / 2;
        Top = owner.Top + (owner.Height - Height) / 2;
    }

    private static string BuildInstructionsText()
    {
        return
            "The objective of this program is to examine a simplified PID control loop used in an" + Environment.NewLine +
            "industrial setting, in this case, a water tank with level control." + Environment.NewLine +
            Environment.NewLine +
            "Because there are so many variations to PID control loops between manufacturers of" + Environment.NewLine +
            "controllers, I have chosen a basic algorithm that will allow you to experiment with each" + Environment.NewLine +
            "of the 3 PID variables." + Environment.NewLine +
            Environment.NewLine +
            "The water tank is constructed of a manual inlet valve that the machine operator can" + Environment.NewLine +
            "control, a water tank with a level sensor, and an outlet valve that can either be controlled" + Environment.NewLine +
            "manually, or be controlled by the computer using the PID control algorithm." + Environment.NewLine +
            Environment.NewLine +
            "When the program starts, the system is already placed into automatic mode with a somewhat" + Environment.NewLine +
            "stable setting for the PID algorithm. The operator can alter the inlet valve position and see" + Environment.NewLine +
            "how the computer adjusts the output valve to maintain the level setpoint." + Environment.NewLine +
            Environment.NewLine +
            "1) Alter the manual input valve and watch the PID loop in action." + Environment.NewLine +
            Environment.NewLine +
            "2) With the manual input valve fully opened (100%), click the button for unstable water supply." + Environment.NewLine +
            "   This button causes the main supply water to fluctuate, as in a real process." + Environment.NewLine +
            "   Watch the PID loop make slight adjustments to correct the unstable supply." + Environment.NewLine +
            Environment.NewLine +
            "3) Put the system in MANUAL CONTROL." + Environment.NewLine +
            "   You can now control both the inlet valve AND the outlet valve." + Environment.NewLine +
            "   See how well you can manually control the level with an unstable water supply." + Environment.NewLine +
            Environment.NewLine +
            "4) Make changes to the level setpoint (SP) and watch the PID loop control to the new SP." + Environment.NewLine +
            "   When you change PID variables, create an upset by changing the setpoint (SP)." + Environment.NewLine +
            "   This shows, via the graphs, how well you are tuning the control loop." + Environment.NewLine +
            Environment.NewLine +
            "Proportional (GAIN): 0-100%, how much to amplify the output based on the error (SP-PV)." + Environment.NewLine +
            "Integral (RESET): In seconds (also called repeats per minute), how often error is reset and" + Environment.NewLine +
            "   values are re-evaluated. This removes offset between PV and SP." + Environment.NewLine +
            "Derivative (RATE): In seconds, the amount of time that advances the output. This can be" + Environment.NewLine +
            "   used to adjust processes with lag/sluggish response or processes sensitive to output changes.";
    }
}

