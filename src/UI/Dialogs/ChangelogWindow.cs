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
                    "Nothing so far, just got the app auto-build working tho.\n" +
                    "================================================================================================================================\n" +
                    "6/15/20 - Build 1\n" +
                    "* Crashes due to invalid settings files are now resolved (again because I did it wrong last update).\n" +
                    "* Default save format settings are now available for crash backups `Settings -> Other`.\n" +
                    "* Crash Backups now save in the format `## Crash Backup month.day.year_hours.minutes.filetype`.\n" +
                    "* Fixed a issue where autosaves and quicksaves were saved as `## XXXXsave_day.month.year_hours.minutes.filetype`, not `## XXXXsave_month.day.year_hours.minutes.filetype`.\n" +
                    "* Updated the changelog to use it's own window.\n"+
                    "* Fixed a bug where recordings end 1 frame too early (added a extra frame to every new recording).\n" +
                    "* Fixed editing Color Triggers, they now show the correct color after editing.\n*" +
                    " You can now drag and drop / open with `.trk`, `.json` and `.sol` files with `linerider.exe` to automatically open them!\n" +
                    "* Added a hotkeys to Draw the Debug Grid and Debug Camera (`,` and `.`).\n" +
                    "* Custom X and Y Gravity, custom Gravity Well sizes and starting colors are now editable in `Track Properties`.\n" +
                    "--* This modifies the save format so **the `.trk` files LRT saves will not be compatible with LRA or other LRA mods past this update if you use the new features**.\n" +
                    "--* **However `.json` files saved in LRT will continue to work in LRA or LRA mods regardless** even with the extra features added to the file.\n" +
                    "--* Also custom Gravity Well sizes will modify the box Bosh uses to check for collisions, use the Debug Grid to see this change. \n" +
                    "----* The grid is not accurate on the first frame, this is a bug.\n" +
                    "\n" +
                    "================================================================================================================================\n" +
                    "\n" +
                    "6/16/20 - Build 1\n"+
                    "* Fixed a issue where the Gravity Well size was not being set when loading a new track.\n" +
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
