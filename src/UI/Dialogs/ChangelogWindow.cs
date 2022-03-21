using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;

namespace linerider.UI
{
    public class ChangelogWindow : DialogBase
    {
        public ChangelogWindow(GameCanvas parent, Editor editor) : base(parent, editor)
        {
            Title = "Changelog for " + Program.Version;
            AutoSizeToContents = false;
            SetSize(1100, 380);

            var changelogText = "" +
                    "================================================================================================================================\n" +
                    "Linux Build Changelog\n" +
                    "* Removed some changelogs. To see them, click see 'previous changes'.\n" +
                    "* Added updater script for when Pi-Apps does not do it automatically.\n" +
                    "* Just finished adding the changelog, currently, all bugs are fixed.\n" +
                    "================================================================================================================================\n" +
                    "\n" +
                    "NOTE: Discord is *still* auto disabled on startup for now until I reimplement it in a more stable way.";


            ControlBase bottomcontainer = new ControlBase(this)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true
            };

            Button btncontinue = new Button(null)
            {
                Text = "Continue",
                Name = "btncontinue",
                Dock = Dock.Right,
                Margin = new Margin(10, 0, 0, 0),
                AutoSizeToContents = true,
            };
            btncontinue.Clicked += (o, e) =>
            {
                Close();
            };
            
            Button btndontshow = new Button(null)
            {
                Text = "Continue and don\'t show again",
                Name = "btndontshow",
                Dock = Dock.Right,
                Margin = new Margin(10, 0, 0, 0),
                AutoSizeToContents = true,
            };
            btndontshow.Clicked += (o, e) =>
            {
                Settings.showChangelog = false;
                Settings.Save();
                Close();
            };
            
            Button btngithub = new Button(null)
            {
                Text = "Previous Changelogs (Github)",
                Name = "btngithub",
                Dock = Dock.Right,
                Margin = new Margin(10, 0, 0, 0),
                AutoSizeToContents = true,
            };
            btngithub.Clicked += (o, e) =>
            {
                try
                {
                    GameCanvas.OpenUrl(@"https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Changelogs");
                }
                catch
                {
                    MessageBox.Show(parent, "Unable to open your browser.", "Error!");
                }
                Close();
            };

            ControlBase buttoncontainer = new ControlBase(bottomcontainer)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
                Children =
                {
                    btncontinue,
                    btndontshow,
                    btngithub,
                }
            };
            
            RichLabel l = new RichLabel(this);
            l.Dock = Dock.Top;
            l.AutoSizeToContents = true;
            l.AddText(changelogText, Skin.Colors.Text.Foreground);
            MakeModal(true);
            DisableResizing();
        }
        
        private void CreateLabeledControl(ControlBase parent, string label, ControlBase control)
        {
            control.Dock = Dock.Right;
            ControlBase container = new ControlBase(parent)
            {
                Children =
                {
                    new Label(null)
                    {
                        Text = label,
                        Dock = Dock.Left,
                        Alignment = Pos.Left | Pos.CenterV,
                        Margin = new Margin(0,0,10,0)
                    },
                    control
                },
                AutoSizeToContents = true,
                Dock = Dock.Top,
                Margin = new Margin(0, 1, 0, 1)
            };
        }
    }
}
