﻿//#define debuggrid
//#define debugcamera
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using Gwen;
using Gwen.Controls;
using linerider.Audio;
using linerider.Drawing;
using linerider.Rendering;
using linerider.IO;
using linerider.Tools;
using linerider.UI;
using linerider.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Key = OpenTK.Input.Key;
using Label = Gwen.Controls.Label;
using Menu = Gwen.Controls.Menu;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using MessageBox = Gwen.Controls.MessageBox;
using linerider.Game;
using System.Windows.Forms.VisualStyles;
using System.IO;
using System.Linq;
using System.Configuration;
using linerider.Addons;

namespace linerider
{
    public class MainWindow : OpenTK.GameWindow
    {
        public Discord.Discord discord = null; //Create discord for game sdk activity
        public static int startTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;  //Probably a easier way to do this but whatever
        public int lastUpdateTime = 0; //Last time the activity was updated
        public bool firstGameUpdate = true; //Run this only on the first update (probably a better way to do this, this is probably bad)
        public String curentScarf = null; //What the current scarf it to compare it to the settings
        public bool scarfNeedsUpdate = true; //If the scarf needs a update 
        public String currentBoshSkin = null; //What the current rider skin is to to compare it to the settings
        public bool editBoshPng = Settings.customScarfOnPng; //Local copy of customScarfOnPng to check back
        public bool forceDiscordUpdate = true;

        public Dictionary<string, MouseCursor> Cursors = new Dictionary<string, MouseCursor>();
        public MsaaFbo MSAABuffer;
        public GameCanvas Canvas;
        public bool ReversePlayback = false;

        public Vector2d MouseGamePos = new Vector2d(0.0, 0.0);
        public Size RenderSize
        {
            get
            {
                if (TrackRecorder.Recording)
                {
                    return new Size(Settings.RecordingWidth, Settings.RecordingHeight);
                }
                else if (TrackRecorder.RecordingScreenshot)
                {
                    return new Size(Settings.ScreenshotWidth, Settings.ScreenshotHeight);
                }
                return ClientSize;
            }
            set
            {
                ClientSize = value;
            }
        }
        public Vector2d ScreenTranslation => -ScreenPosition;
        public Vector2d ScreenPosition
            => Track.Camera.GetViewport(
                Track.Zoom,
                RenderSize.Width,
                RenderSize.Height).Vector;

        public Editor Track { get; }
        private bool _uicursor = false;
        private Gwen.Input.OpenTK _input;
        private bool _dragRider;
        private bool _invalidated;
        private readonly Stopwatch _autosavewatch = Stopwatch.StartNew();
        public MainWindow()
            : base(
                Settings.mainWindowWidth,
                Settings.mainWindowHeight,
                new GraphicsMode(new ColorFormat(24), 0, 0, 0, ColorFormat.Empty),
                   "Line Rider: Advanced",
                   GameWindowFlags.Default,
                   DisplayDevice.Default,
                   2,
                   0,
                   GraphicsContextFlags.Default)
        {
            SafeFrameBuffer.Initialize();
            Track = new Editor();
            VSync = VSyncMode.On;
            Context.ErrorChecking = false;
            WindowBorder = WindowBorder.Resizable;
            RenderFrame += (o, e) => { Render(); };
            UpdateFrame += (o, e) => { GameUpdate(); };
            new Thread(AutosaveThreadRunner) { IsBackground = true, Name = "Autosave" }.Start();
            GameService.Initialize(this);
            AddonManager.Initialize(this);
            RegisterHotkeys();
        }

        public override void Dispose()
        {
            if (Canvas != null)
            {
                Canvas.Dispose();
                Canvas.Skin.Dispose();
                Canvas.Skin.DefaultFont.Dispose();
                Canvas.Renderer.Dispose();
                Canvas = null;
            }
            base.Dispose();
        }

        public bool ShouldXySnap()
        {
            return Settings.Editor.ForceXySnap || InputUtils.CheckPressed(Hotkey.ToolXYSnap);
        }
        public void Render(float blend = 1)
        {
            bool shouldrender = _invalidated ||
             Canvas.NeedsRedraw ||
            (Track.Playing) ||
            Canvas.Loading ||
            Track.NeedsDraw ||
            CurrentTools.SelectedTool.NeedsRender;
            if (shouldrender)
            {
                _invalidated = false;
                BeginOrtho();
                if (blend == 1 && Settings.SmoothPlayback && Track.Playing && !Canvas.Scrubbing)
                {
                    blend = Math.Min(1, (float)Track.Scheduler.ElapsedPercent);
                    if (ReversePlayback)
                        blend = 1 - blend;
                    Track.Camera.BeginFrame(blend, Track.Zoom);
                }
                else
                {
                    Track.Camera.BeginFrame(blend, Track.Zoom);
                }
                if (Track.Playing && CurrentTools.PencilTool.Active)
                {
                    CurrentTools.PencilTool.OnMouseMoved(InputUtils.GetMouse());
                }
                if ((Settings.PreviewMode || TrackRecorder.Recording) && !(TrackRecorder.Recording && !Settings.Recording.EnableColorTriggers))
                {
                    /* BG triggers and Line trigger updates */
                    if (Track.Offset == 0)
                    {
                        linerider.Utils.Constants.TriggerBGColor = new Color4((byte)Track.StartingBGColorR, (byte)Track.StartingBGColorG, (byte)Track.StartingBGColorB, (byte)255);
                        linerider.Utils.Constants.StaticTriggerBGColor = new Color4((byte)Track.StartingBGColorR, (byte)Track.StartingBGColorG, (byte)Track.StartingBGColorB, (byte)255);
                        linerider.Utils.Constants.StaticTriggerLineColorChange = Color.FromArgb(255, Track.StartingLineColorR, Track.StartingLineColorG, Track.StartingLineColorB);
                        linerider.Utils.Constants.TriggerLineColorChange = Color.FromArgb(255, Track.StartingLineColorR, Track.StartingLineColorG, Track.StartingLineColorB);
                        GL.ClearColor(Constants.TriggerBGColor);
                    }
                    else
                    {
                        Constants.TriggerBGColor = Track.Timeline.GetFrameBackgroundColor(Track.Offset);
                        GL.ClearColor(Constants.TriggerBGColor);
                    }
                }
                else
                {
                    GL.ClearColor(Settings.NightMode ? Constants.ColorNightMode : (Settings.WhiteBG ? Constants.ColorWhite : Constants.ColorOffwhite));
                    if (Settings.NightMode)
                    {
                        Constants.TriggerLineColorChange = Settings.Lines.DefaultNightLine;
                    }
                    else
                    {
                        Constants.TriggerLineColorChange = Settings.Lines.DefaultLine;
                    }
                }

                MSAABuffer.Use(RenderSize.Width, RenderSize.Height);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Enable(EnableCap.Blend);

                if (Settings.DrawFloatGrid)
                {
                    GameRenderer.DrawFloatGrid();
                }

                if ((InputUtils.Check(Hotkey.DrawDebugGrid) && !TrackRecorder.Recording) || Settings.DrawCollisionGrid)
                {
                    GameRenderer.DbgDrawGrid();
                }

                if ((InputUtils.Check(Hotkey.DrawDebugGrid) && !TrackRecorder.Recording) || Settings.DrawAGWs)
                {
                    GameRenderer.DrawAGWs();
                }

                Track.Render(blend);

                if ((InputUtils.Check(Hotkey.DrawDebugCamera) && !TrackRecorder.Recording) || Settings.DrawCamera)
                {
                    GameRenderer.DbgDrawCamera();
                }

                Canvas.RenderCanvas();
                MSAABuffer.End();

                if (Settings.NightMode)
                {
                    StaticRenderer.RenderRect(new FloatRect(0, 0, RenderSize.Width, RenderSize.Height), Color.FromArgb(40, 0, 0, 0));
                }
                SwapBuffers();
                //there are machines and cases where a refresh may not hit the screen without calling glfinish...
                GL.Finish();
                var seconds = Track.FramerateWatch.Elapsed.TotalSeconds;
                Track.FramerateCounter.AddFrame(seconds);
                Track.FramerateWatch.Restart();
            }
            if (!Focused && !TrackRecorder.Recording)
            {
                Thread.Sleep(16);
            }
            else
            if (!Track.Playing &&
                    !Canvas.NeedsRedraw &&
                    !Track.NeedsDraw &&
                    !CurrentTools.SelectedTool.Active)
            {
                Thread.Sleep(10);
            }
        }
        private void GameUpdateHandleInput()
        {
            if (InputUtils.HandleMouseMove(out int x, out int y) && !Canvas.IsModalOpen)
            {
                CurrentTools.SelectedTool.OnMouseMoved(new Vector2d(x, y));
            }
        }
        /// <summary>
        /// Indefinitely run the autosave function
        /// </summary>
        private void AutosaveThreadRunner()
        {
            while (true)
            {
                Thread.Sleep(1000 * 60 * Settings.autosaveMinutes); // Settings.autosaveMinutes minutes
                try
                {
                    Track.BackupTrack(false);
                }
                catch
                {
                    // do nothing
                }
            }
        }
        public void GameUpdate()
        {
            //TODO: Put these not in the main loop and put them in reasonable places
            if (firstGameUpdate)
            {
                Canvas.ShowChangelog();
                firstGameUpdate = false;
                removeAllScarfColors(); //Remove default white scarf
                reloadRiderModel();
                forceDiscordUpdate = true;
                Settings.discordActivityEnabled = false; //Dumb but I'm doing this in case it leads to the app not starting due to discord not being open
            }

            //Code to run each frame
            int currentTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; //Get current time for discord activity
            //Debug.WriteLine(Track.Name);
            //Update bosh skin if needed
            if (currentBoshSkin != Settings.SelectedBoshSkin)
            {
                reloadRiderModel();
                removeAllScarfColors();
                updateScarf();
                currentBoshSkin = Settings.SelectedBoshSkin;
                editBoshPng = Settings.customScarfOnPng;
            }
            //Update scarf if needed
            if ((scarfNeedsUpdate || (curentScarf != Settings.SelectedScarf)) || ((Settings.customScarfOnPng == false) && (editBoshPng)))
            {
                curentScarf = Settings.SelectedScarf;
                removeAllScarfColors();
                updateScarf();
                scarfNeedsUpdate = false;
                editBoshPng = Settings.customScarfOnPng;
                if (Settings.customScarfOnPng) { reloadRiderModel(); }

                while (getScarfColorList().Count() < Settings.ScarfSegments)
                {
                    getScarfColorList().AddRange(getScarfColorList());
                    getScarfOpacityList().AddRange(getScarfOpacityList());
                }

                for (int i = 1; i < Settings.multiScarfAmount; i++)
                {
                    insertScarfColor(0x0000FF, 0x00, ((i * Settings.multiScarfSegments)) + (i - 1) - (1 + i));
                }
            }
            //If edits to the png is toggled update the rider
            if (editBoshPng != Settings.customScarfOnPng)
            {
                reloadRiderModel();
                editBoshPng = Settings.customScarfOnPng;
            }
            //If the discord activity should be updated
            if ((((currentTime % 10 == 0) && (currentTime != lastUpdateTime)) || forceDiscordUpdate) && Settings.discordActivityEnabled)
            {
                if (discord == null)
                {
                    discord = new Discord.Discord(506953593945980933, (UInt64)Discord.CreateFlags.Default);
                    discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
                    {
                        Console.WriteLine("Log[{0}] {1}", level, message);
                    });
                }
                lastUpdateTime = currentTime;
                UpdateActivity(discord);
                forceDiscordUpdate = false;
            }
            //Update each frame
            if (Settings.discordActivityEnabled)
            {
                try { discord.RunCallbacks(); }
                catch (Exception e) { Debug.WriteLine(e); }
            }

            //Regular code starts here
            GameUpdateHandleInput();
            var updates = Track.Scheduler.UnqueueUpdates();
            if (updates > 0)
            {
                Invalidate();
                if (Track.Playing)
                {
                    if (InputUtils.Check(Hotkey.PlaybackZoom))
                        Track.ZoomBy(0.08f);
                    else if (InputUtils.Check(Hotkey.PlaybackUnzoom))
                        Track.ZoomBy(-0.08f);
                }
            }

            if (Track.Playing)
            {
                if (ReversePlayback)
                {
                    for (int i = 0; i < updates; i++)
                    {
                        Track.PreviousFrame();
                        Track.UpdateCamera(true);
                    }
                }
                else
                {
                    Track.Update(updates);
                }
            }
            if (Program.NewVersion != null)
            {
                Canvas.ShowOutOfDate();
            }
            AudioService.EnsureSync();
        }

        public void reloadRiderModel()
        {
            if (Settings.SelectedBoshSkin == null) { Models.LoadModels(); return; }

            Bitmap bodyPNG = null;
            Bitmap bodyDeadPNG = null;

            try
            {
                if (Settings.customScarfOnPng && !Settings.SelectedBoshSkin.Equals("*default*"))
                {
                    bodyPNG = new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/body.png");
                    bodyDeadPNG = new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/bodydead.png");
                    Bitmap palettePNG = new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/palette.png");
                    var scarfColorList = getScarfColorList();
                    if (scarfColorList.Count == 0) { Models.LoadModels(); return; }
                    for (int i = 0; i < palettePNG.Width; i++)
                    {
                        Color colorToChange = palettePNG.GetPixel(i, 0);
                        colorToChange = Color.FromArgb(255, colorToChange.R, colorToChange.G, colorToChange.B);

                        Color newScarfColor = Color.FromArgb(scarfColorList[i % scarfColorList.Count]);
                        newScarfColor = Color.FromArgb(255, newScarfColor); //Add 255 alpha

                        for (int x = 0; x < bodyPNG.Width; x++)
                        {
                            for (int y = 0; y < bodyPNG.Height; y++)
                            {
                                Color aliveColor = bodyPNG.GetPixel(x, y);
                                if (aliveColor.Equals(colorToChange))
                                {
                                    bodyPNG.SetPixel(x, y, newScarfColor);
                                }
                                Color deadColor = bodyDeadPNG.GetPixel(x, y);
                                if (deadColor.Equals(colorToChange))
                                {
                                    bodyDeadPNG.SetPixel(x, y, newScarfColor);
                                }
                            }//for y
                        }//for x
                    }//for each (i)
                    shiftScarfColors((scarfColorList.Count * palettePNG.Width) - palettePNG.Width);
                }//if
            }
            catch (Exception e) { Debug.WriteLine(e); Models.LoadModels(); }

            if (Settings.SelectedBoshSkin == "*default*") { Models.LoadModels(); return; }

            try
            {
                if (bodyPNG == null) { bodyPNG = new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/body.png"); }
                if (bodyDeadPNG == null) { bodyDeadPNG = new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/bodydead.png"); }

                Models.LoadModels(
                    bodyPNG,
                    bodyDeadPNG,
                    new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/sled.png"),
                    new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/brokensled.png"),
                    new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/arm.png"),
                    new Bitmap(Program.UserDirectory + "/Riders/" + Settings.SelectedBoshSkin + "/leg.png"));
            }
            catch (Exception e) { Debug.WriteLine(e); Models.LoadModels(); }
        }

        public void updateScarf()
        {
            string scarfLocation = Program.UserDirectory + "/Scarves/" + Settings.SelectedScarf;
            try
            {
                if ((Settings.SelectedScarf != "*default*") && (File.ReadLines(scarfLocation).First() == "#LRTran Scarf File"))
                {
                    string[] lines = File.ReadAllLines(scarfLocation);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        //Debug.WriteLine(lines[i]);
                        int color = Convert.ToInt32(lines[i].Substring(0, lines[i].IndexOf(",")), 16);
                        byte opacity = Convert.ToByte(lines[i].Substring(lines[i].IndexOf(" ") + 1), 16);
                        //Debug.WriteLine("Color: " + color);
                        //Debug.WriteLine("Opacity: " + opacity);
                        addScarfColor(color, opacity);
                    }
                }
                else { addScarfColor(0xff6464, 0xff); /*Default Color 1*/ addScarfColor(0xD10101, 0xff); /*Default Color 2*/}
            }
            catch { addScarfColor(0xff6464, 0xff); /*Default Color 1*/ addScarfColor(0xD10101, 0xff); /*Default Color 2*/}
        }
        //Used to be static
        public void UpdateActivity(Discord.Discord discord)
        {
            String toolName = (linerider.Tools.CurrentTools.SelectedTool.ToString().Substring(16)); toolName = toolName.Substring(0, toolName.Length - 4).ToLower();

            String versionText = "LRA:CE version " + linerider.Program.Version;

            String largeKey = Settings.largeImageKey;
            String largeText = versionText + " ==================== Source code: https://github.com/Sussy-OS/LRA-Community-Edition";
            String smallKey = toolName;
            String smallText = "Currently using the " + toolName + " tool";

            String setting1 = discordSettingToString(Settings.discordActivity1);
            String setting2 = discordSettingToString(Settings.discordActivity2);
            String setting3 = discordSettingToString(Settings.discordActivity3);
            String setting4 = discordSettingToString(Settings.discordActivity4);

            String detailsText = setting1;
            if (setting2.Length > 0) { detailsText = detailsText + " | " + setting2; }
            String stateText = setting3;
            if (setting4.Length > 0) { stateText = stateText + " | " + setting4; }

            var activityManager = discord.GetActivityManager();
            var lobbyManager = discord.GetLobbyManager();

            var activity = new Discord.Activity
            {
                Type = 0,
                Details = detailsText,
                State = stateText,
                Timestamps =
                {
                    Start = startTime,
                    End = 0,
                },
                Assets =
            {
                LargeImage = largeKey,
                LargeText = largeText,
                SmallImage = smallKey,
                SmallText = smallText,
            },
                Instance = false
            };

            activityManager.UpdateActivity(activity, result =>
            {
                Console.WriteLine("Update Activity {0}", result);
            });
        }

        public String discordSettingToString(String setting)
        {
            String toolName = (linerider.Tools.CurrentTools.SelectedTool.ToString().Substring(16)); toolName = toolName.Substring(0, toolName.Length - 4).ToLower();
            String lineText = "Amount of Lines: " + Track.LineCount;
            String unsavedChangesText = "Unsaved changes: " + Track.TrackChanges;
            String toolText = "Currently using the " + toolName + " tool";
            String trackText = "Track name: \"" + Track.Name + "\"";
            String versionText = "LRA:CE version " + linerider.Program.Version;

            switch (setting)
            {
                case "none":
                    return "";
                case "lineText":
                    return lineText;
                case "unsavedChangesText":
                    return unsavedChangesText;
                case "toolText":
                    return toolText;
                case "trackText":
                    return trackText;
                case "versionText":
                    return versionText;
                default:
                    return "";
            }
        }

        public void Invalidate()
        {
            _invalidated = true;
        }
        public void UpdateCursor()
        {
            MouseCursor cursor;

            if (_uicursor)
                cursor = Canvas.Platform.CurrentCursor;
            else if (Track.Playing || _dragRider)
                cursor = Cursors["default"];
            else if (CurrentTools.SelectedTool != null)
                cursor = CurrentTools.SelectedTool.Cursor;
            else
            {
                cursor = MouseCursor.Default;
                Debug.Fail("Improperly handled UpdateCursor");
            }
            if (cursor != Cursor)
            {
                Cursor = cursor;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            Shaders.Load();
            MSAABuffer = new MsaaFbo();
            var renderer = new Gwen.Renderer.OpenTK();

            var skinpng = renderer.CreateTexture(GameResources.DefaultSkin);

            var fontpng = renderer.CreateTexture(GameResources.liberation_sans_15_png);
            var fontpngbold = renderer.CreateTexture(GameResources.liberation_sans_15_bold_png);

            var gamefont_15 = new Gwen.Renderer.BitmapFont(
                renderer,
                GameResources.liberation_sans_15_fnt,
                fontpng);


            var gamefont_15_bold = new Gwen.Renderer.BitmapFont(
                renderer,
                GameResources.liberation_sans_15_bold_fnt,
                fontpngbold);

            var skin = new Gwen.Skin.TexturedBase(renderer,
            skinpng,
            GameResources.DefaultColors
            )
            { DefaultFont = gamefont_15 };

            Fonts f = new Fonts(gamefont_15, gamefont_15_bold);
            Canvas = new GameCanvas(skin,
            this,
            renderer,
            f);

            _input = new Gwen.Input.OpenTK(this);
            _input.Initialize(Canvas);
            Canvas.ShouldDrawBackground = false;
            Models.LoadModels();

            AddCursor("pencil", GameResources.cursor_pencil, 6, 25);
            AddCursor("line", GameResources.cursor_line, 11, 11);
            AddCursor("eraser", GameResources.cursor_eraser, 8, 8);
            AddCursor("hand", GameResources.cursor_move, 16, 16);
            AddCursor("hand_point", GameResources.cursor_hand, 14, 8);
            AddCursor("closed_hand", GameResources.cursor_dragging, 16, 16);
            AddCursor("adjustline", GameResources.cursor_select, 4, 4);
            AddCursor("size_nesw", GameResources.cursor_size_nesw, 16, 16);
            AddCursor("size_nwse", GameResources.cursor_size_nwse, 16, 16);
            AddCursor("size_hor", GameResources.cursor_size_horz, 16, 16);
            AddCursor("size_ver", GameResources.cursor_size_vert, 16, 16);
            AddCursor("size_all", GameResources.cursor_size_all, 16, 16);
            AddCursor("default", GameResources.cursor_default, 7, 4);
            AddCursor("zoom", GameResources.cursor_zoom_in, 11, 10);
            AddCursor("ibeam", GameResources.cursor_ibeam, 11, 11);
            Program.UpdateCheck();
            Track.AutoLoadPrevious();
            linerider.Tools.CurrentTools.Init();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Track.Camera.OnResize();
            try
            {
                Canvas.SetCanvasSize(RenderSize.Width, RenderSize.Height);
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                var r = _input.ProcessMouseMessage(e);
                _uicursor = _input.MouseCaptured;
                if (Canvas.GetOpenWindows().Count != 0)
                {
                    UpdateCursor();
                    return;
                }
                if (!r)
                {
                    InputUtils.ProcessMouseHotkeys();
                    if (!Track.Playing)
                    {
                        bool dragstart = false;
                        MouseGamePos = ScreenPosition + (new Vector2d(e.X, e.Y) / Track.Zoom);
                        if (Track.Offset == 0 &&
                         e.Button == MouseButton.Left &&
                        InputUtils.Check(Hotkey.EditorMoveStart))
                        {
                            dragstart = Game.Rider.GetBounds(
                                Track.GetStart()).Contains(
                                    MouseGamePos.X,
                                    MouseGamePos.Y);
                            if (dragstart)
                            {
                                // 5 is arbitrary, but i assume that's a decent
                                // place to assume the user has done "work"
                                if (!Track.MoveStartWarned && Track.LineCount > 5)
                                {
                                    var popup = MessageBox.Show(Canvas,
                                        "You're about to move the start position of the rider." +
                                        " This cannot be undone, and may drastically change how your track plays." +
                                        "\nAre you sure you want to do this?", "Warning", MessageBox.ButtonType.OkCancel);
                                    popup.RenameButtons("I understand");
                                    popup.Dismissed += (o, args) =>
                                    {
                                        if (popup.Result == Gwen.DialogResult.OK)
                                        {
                                            Track.MoveStartWarned = true;
                                        }
                                    };
                                }
                                else
                                {
                                    _dragRider = dragstart;
                                }
                            }
                        }
                        if (!_dragRider && !dragstart)
                        {
                            if (e.Button == MouseButton.Left)
                            {
                                CurrentTools.SelectedTool.OnMouseDown(new Vector2d(e.X, e.Y));
                            }
                            else if (e.Button == MouseButton.Right)
                            {
                                CurrentTools.SelectedTool.OnMouseRightDown(new Vector2d(e.X, e.Y));
                            }
                        }

                    }
                    else if (CurrentTools.SelectedTool == CurrentTools.PencilTool && CurrentTools.PencilTool.DrawingScenery)
                    {
                        if (e.Button == MouseButton.Left)
                        {
                            CurrentTools.PencilTool.OnMouseDown(new Vector2d(e.X, e.Y));
                        }
                    }
                }
                UpdateCursor();
                Invalidate();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                _dragRider = false;
                var r = _input.ProcessMouseMessage(e);
                _uicursor = _input.MouseCaptured;
                InputUtils.CheckCurrentHotkey();
                if (!r || CurrentTools.SelectedTool.IsMouseButtonDown)
                {
                    if (!CurrentTools.SelectedTool.IsMouseButtonDown &&
                        Canvas.GetOpenWindows().Count != 0)
                    {
                        UpdateCursor();
                        return;
                    }
                    if (e.Button == MouseButton.Left)
                    {
                        CurrentTools.SelectedTool.OnMouseUp(new Vector2d(e.X, e.Y));
                    }
                    else if (e.Button == MouseButton.Right)
                    {
                        CurrentTools.SelectedTool.OnMouseRightUp(new Vector2d(e.X, e.Y));
                    }
                }
                UpdateCursor();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            try
            {
                var pos = new Vector2d(e.X, e.Y);
                MouseGamePos = ScreenPosition + (pos / Track.Zoom);
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                var r = _input.ProcessMouseMessage(e);
                _uicursor = _input.MouseCaptured;
                if (Canvas.GetOpenWindows().Count != 0)
                {
                    UpdateCursor();
                    return;
                }
                if (_dragRider)
                {
                    Track.Stop();
                    using (var trk = Track.CreateTrackWriter())
                    {
                        trk.Track.StartOffset = MouseGamePos;
                        Track.Reset();
                        Track.NotifyTrackChanged();
                    }
                    Invalidate();
                }
                if (CurrentTools.SelectedTool.RequestsMousePrecision)
                {
                    CurrentTools.SelectedTool.OnMouseMoved(new Vector2d(e.X, e.Y));
                }

                if (r)
                {
                    Invalidate();
                }
                UpdateCursor();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                if (_input.ProcessMouseMessage(e))
                    return;
                if (Canvas.GetOpenWindows().Count != 0)
                {
                    UpdateCursor();
                    return;
                }
                var delta = (float.IsNaN(e.DeltaPrecise) ? e.Delta : e.DeltaPrecise);
                delta *= Settings.ScrollSensitivity;
                Track.ZoomBy(delta / 6);
                UpdateCursor();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            try
            {
                if (!e.IsRepeat)
                {
                    InputUtils.KeyDown(e.Key);
                }
                InputUtils.UpdateKeysDown(e.Keyboard, e.Modifiers);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                var mod = e.Modifiers;
                if (_input.ProcessKeyDown(e))
                {
                    return;
                }
                if (e.Key == Key.Escape && !e.IsRepeat)
                {
                    var openwindows = Canvas.GetOpenWindows();
                    if (openwindows != null && openwindows.Count >= 1)
                    {
                        foreach (var v in openwindows)
                        {
                            ((WindowControl)v).Close();
                            Invalidate();
                        }
                        return;
                    }
                }
                if (
                    Canvas.IsModalOpen ||
                    (!Track.Playing && CurrentTools.SelectedTool.OnKeyDown(e.Key)) ||
                    _dragRider)
                {
                    UpdateCursor();
                    Invalidate();
                    return;
                }
                InputUtils.ProcessKeyboardHotkeys();
                UpdateCursor();
                Invalidate();
                var input = e.Keyboard;
                if (!input.IsAnyKeyDown)
                    return;
                if (input.IsKeyDown(Key.AltLeft) || input.IsKeyDown(Key.AltRight))
                {
                    if (input.IsKeyDown(Key.Enter))
                    {
                        if (WindowBorder == WindowBorder.Resizable)
                        {
                            WindowBorder = WindowBorder.Hidden;
                            X = 0;
                            Y = 0;
                            var area = Screen.PrimaryScreen.Bounds;
                            RenderSize = area.Size;
                        }
                        else
                        {
                            WindowBorder = WindowBorder.Resizable;
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            try
            {
                InputUtils.UpdateKeysDown(e.Keyboard, e.Modifiers);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                InputUtils.CheckCurrentHotkey();
                CurrentTools.SelectedTool.OnKeyUp(e.Key);
                _input.ProcessKeyUp(e);
                UpdateCursor();
                Invalidate();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }


        public void StopTools()
        {
            CurrentTools.SelectedTool.Stop();
        }
        public void StopHandTool()
        {
            if (CurrentTools.SelectedTool == CurrentTools.HandTool)
            {
                CurrentTools.HandTool.Stop();
            }
        }

        private void BeginOrtho()
        {
            if (RenderSize.Height > 0 && RenderSize.Width > 0)
            {
                GL.Viewport(new Rectangle(0, 0, RenderSize.Width, RenderSize.Height));
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, RenderSize.Width, RenderSize.Height, 0, 0, 1);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();
            }
        }

        private void AddCursor(string name, Bitmap image, int hotx, int hoty)
        {
            var data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppPArgb);
            Cursors[name] = new MouseCursor(hotx, hoty, image.Width, image.Height, data.Scan0);
            image.UnlockBits(data);
        }
        private void RegisterHotkeys()
        {
            RegisterPlaybackHotkeys();
            RegisterEditorHotkeys();
            RegisterSettingHotkeys();
            RegisterPopupHotkeys();
        }
        private void RegisterSettingHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.PreferenceOnionSkinning, () => true, () =>
            {
                Settings.OnionSkinning = !Settings.OnionSkinning;
                Settings.Save();
                Track.Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.PreferenceAllCheckboxSettings, () => true, () =>
            {
                if (Settings.Editor.DrawContactPoints || Settings.Editor.MomentumVectors || Settings.Editor.HitTest || Settings.Editor.RenderGravityWells)
                {
                    Settings.Editor.DrawContactPoints = false;
                    Settings.Editor.MomentumVectors = false;
                    Settings.Editor.HitTest = false;
                    Settings.Editor.RenderGravityWells = false;
                }
                else
                {
                    Settings.Editor.DrawContactPoints = true;
                    Settings.Editor.MomentumVectors = true;
                    Settings.Editor.HitTest = true;
                    Settings.Editor.RenderGravityWells = true;
                }
                Settings.Save();
                Track.Invalidate();
            });

            InputUtils.RegisterHotkey(Hotkey.InvisibleRider, () => true, () =>
            {
                Settings.InvisibleRider = !Settings.InvisibleRider;
                Settings.Save();
                Track.Invalidate();
            });

            RegisterAddonSettingHotkeys();
        }
        private void RegisterAddonSettingHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.MagicAnimateAdvanceFrame, () => true, () =>
            {
                MagicAnimator.AdvanceFrame();
            });
            InputUtils.RegisterHotkey(Hotkey.MagicAnimateRecedeFrame, () => true, () =>
            {
                MagicAnimator.RecedeFrame();
            });
            InputUtils.RegisterHotkey(Hotkey.MagicAnimateRecedeMultiFrame, () => true, () =>
            {
                MagicAnimator.RecedeMultiFrame();
            });
            InputUtils.RegisterHotkey(Hotkey.LineGeneratorWindow, () => true, () =>
            {
                Canvas.ShowGeneratorWindow(MouseGamePos);
            });
        }
        private void RegisterPlaybackHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.PlaybackStartSlowmo, () => true, () =>
            {
                StopTools();
                Track.StartFromFlag();
                Track.Scheduler.UpdatesPerSecond = Settings.SlowmoSpeed;
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStartIgnoreFlag, () => true, () =>
            {
                StopTools();
                Track.StartIgnoreFlag();
                Track.ResetSpeedDefault();
            });
            // InputUtils.RegisterHotkey(Hotkey.PlaybackStartGhostFlag, () => true, () =>
            // {
            //     StopTools();
            //     Track.ResumeFromFlag();
            //     Track.ResetSpeedDefault();
            // });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStart, () => true, () =>
            {
                StopTools();
                Track.StartFromFlag();
                Track.ResetSpeedDefault();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStop, () => true, () =>
            {
                StopTools();
                Track.Stop();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackFlag, () => true, () =>
            {
                Track.Flag(Track.Offset);
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackForward, () => true, () =>
            {
                StopTools();
                if (Track.Paused)
                    Track.TogglePause();
                ReversePlayback = false;
                UpdateCursor();
            },
            () =>
            {
                if (!Track.Paused)
                    Track.TogglePause();
                ReversePlayback = false;
                Track.UpdateCamera();
                UpdateCursor();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackBackward, () => true, () =>
            {
                StopTools();
                if (Track.Paused)
                    Track.TogglePause();
                ReversePlayback = true;
                UpdateCursor();
            },
            () =>
            {
                if (!Track.Paused)
                    Track.TogglePause();
                ReversePlayback = false;
                Track.UpdateCamera();
                UpdateCursor();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackFrameNext, () => true, () =>
            {
                StopHandTool();
                if (!Track.Paused)
                    Track.TogglePause();
                Track.NextFrame();
                Invalidate();
                Track.UpdateCamera();
                if (CurrentTools.SelectedTool.IsMouseButtonDown)
                {
                    CurrentTools.SelectedTool.OnMouseMoved(InputUtils.GetMouse());
                }
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackFramePrev, () => true, () =>
            {
                StopHandTool();
                if (!Track.Paused)
                    Track.TogglePause();
                Track.PreviousFrame();
                Invalidate();
                Track.UpdateCamera(true);
                if (CurrentTools.SelectedTool.IsMouseButtonDown)
                {
                    CurrentTools.SelectedTool.OnMouseMoved(InputUtils.GetMouse());
                }
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackSpeedUp, () => true, () =>
            {
                Track.PlaybackSpeedUp();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackSpeedDown, () => true, () =>
            {
                Track.PlaybackSpeedDown();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackSlowmo, () => true, () =>
            {
                if (Track.Scheduler.UpdatesPerSecond !=
                Settings.SlowmoSpeed)
                {
                    Track.Scheduler.UpdatesPerSecond = Settings.SlowmoSpeed;
                }
                else
                {
                    Track.ResetSpeedDefault(false);
                }
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackTogglePause, () => true, () =>
            {
                StopTools();
                Track.TogglePause();
            },
            null,
            repeat: false);
            InputUtils.RegisterHotkey(Hotkey.PlaybackIterationNext, () => !Track.Playing, () =>
            {
                StopTools();
                if (!Track.Paused)
                    Track.TogglePause();
                if (Track.IterationsOffset != 6)
                {
                    Track.IterationsOffset++;
                }
                else
                {
                    Track.NextFrame();
                    Track.IterationsOffset = 0;
                    Track.UpdateCamera();
                }
                Track.InvalidateRenderRider();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackIterationPrev, () => !Track.Playing, () =>
            {
                if (Track.Offset != 0)
                {
                    StopTools();
                    if (Track.IterationsOffset > 0)
                    {
                        Track.IterationsOffset--;
                    }
                    else
                    {
                        Track.PreviousFrame();
                        Track.IterationsOffset = 6;
                        Invalidate();
                        Track.UpdateCamera();
                    }
                    Track.InvalidateRenderRider();
                }
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackResetCamera, () => true, () =>
            {
                Track.Zoom = Track.Timeline.GetFrameZoom(Track.Offset);
                Track.UseUserZoom = false;
                Track.UpdateCamera();
            });
        }
        private void RegisterPopupHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.LoadWindow, () => true, () =>
            {
                Canvas.ShowLoadDialog();
            });

            InputUtils.RegisterHotkey(Hotkey.PreferencesWindow,
            () => !CurrentTools.SelectedTool.Active,
            () =>
            {
                Canvas.ShowPreferencesDialog();
            });
            InputUtils.RegisterHotkey(Hotkey.GameMenuWindow,
            () => !CurrentTools.SelectedTool.Active,
            () =>
            {
                Canvas.ShowGameMenuWindow();
            });
            InputUtils.RegisterHotkey(Hotkey.TriggerMenuWindow,
            () => !CurrentTools.SelectedTool.Active,
            () =>
            {
                Canvas.ShowTriggerWindow();
            });
            InputUtils.RegisterHotkey(Hotkey.SaveAsWindow,
            () => !CurrentTools.SelectedTool.Active,
            () =>
            {
                Canvas.ShowSaveDialog();
            });
            InputUtils.RegisterHotkey(Hotkey.TrackPropertiesWindow,
            () => !CurrentTools.SelectedTool.Active,
            () =>
            {
                Canvas.ShowTrackPropertiesDialog();
            });
            InputUtils.RegisterHotkey(Hotkey.Quicksave, () => true, () =>
            {
                Track.QuickSave();
            });
        }
        private void RegisterEditorHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.EditorPencilTool, () => !Track.Playing, () =>
            {
                CurrentTools.SetTool(CurrentTools.PencilTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorLineTool, () => !Track.Playing, () =>
            {
                CurrentTools.SetTool(CurrentTools.LineTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorEraserTool, () => !Track.Playing, () =>
            {
                CurrentTools.SetTool(CurrentTools.EraserTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorSelectTool, () => !Track.Playing, () =>
            {
                CurrentTools.SetTool(CurrentTools.MoveTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorPanTool, () => !Track.Playing, () =>
            {
                CurrentTools.SetTool(CurrentTools.HandTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorQuickPan, () => !Track.Playing && !Canvas.IsModalOpen, () =>
            {
                CurrentTools.QuickPan = true;
                Invalidate();
                UpdateCursor();
            },
            () =>
            {
                CurrentTools.QuickPan = false;
                Invalidate();
                UpdateCursor();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorDragCanvas, () => !Track.Playing && !Canvas.IsModalOpen, () =>
            {
                var mouse = InputUtils.GetMouse();
                CurrentTools.QuickPan = true;
                CurrentTools.HandTool.OnMouseDown(new Vector2d(mouse.X, mouse.Y));
            },
            () =>
            {
                if (CurrentTools.QuickPan)
                {
                    var mouse = InputUtils.GetMouse();
                    CurrentTools.HandTool.OnMouseUp(new Vector2d(mouse.X, mouse.Y));
                    CurrentTools.QuickPan = false;
                }
            });

            InputUtils.RegisterHotkey(Hotkey.EditorUndo, () => !Track.Playing, () =>
            {
                CurrentTools.SelectedTool.Cancel();
                var hint = Track.UndoManager.Undo();
                CurrentTools.SelectedTool.OnUndoRedo(true, hint);
                Invalidate();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.EditorRedo, () => !Track.Playing, () =>
            {
                CurrentTools.SelectedTool.Cancel();
                var hint = Track.UndoManager.Redo();
                CurrentTools.SelectedTool.OnUndoRedo(false, hint);
                Invalidate();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.EditorRemoveLatestLine, () => !Track.Playing, () =>
            {
                if (!Track.Playing)
                {
                    StopTools();
                    using (var trk = Track.CreateTrackWriter())
                    {
                        CurrentTools.SelectedTool.Stop();
                        var l = trk.GetNewestLine();
                        if (l != null)
                        {
                            Track.UndoManager.BeginAction();
                            trk.RemoveLine(l);
                            Track.UndoManager.EndAction();
                        }

                        Track.NotifyTrackChanged();
                        Invalidate();
                    }
                }
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.EditorFocusStart, () => !Track.Playing, () =>
            {
                using (var trk = Track.CreateTrackReader())
                {
                    var l = trk.GetOldestLine();
                    if (l != null)
                    {
                        Track.Camera.SetFrameCenter(l.Position);
                        Invalidate();
                    }
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorFocusLastLine, () => !Track.Playing, () =>
            {
                using (var trk = Track.CreateTrackReader())
                {
                    var l = trk.GetNewestLine();
                    if (l != null)
                    {
                        Track.Camera.SetFrameCenter(l.Position);
                        Invalidate();
                    }
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorCycleToolSetting, () => !Track.Playing, () =>
            {
                if (CurrentTools.SelectedTool.ShowSwatch)
                {
                    CurrentTools.SelectedTool.Swatch.IncrementSelectedMultiplier();
                    Invalidate();
                }
            });
            InputUtils.RegisterHotkey(Hotkey.ToolToggleOverlay, () => !Track.Playing, () =>
            {
                Settings.Local.TrackOverlay = !Settings.Local.TrackOverlay;
            });
            InputUtils.RegisterHotkey(Hotkey.EditorToolColor1, () => !Track.Playing, () =>
            {
                var swatch = CurrentTools.SelectedTool.Swatch;
                if (swatch != null)
                {
                    swatch.Selected = LineType.Blue;
                }
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorToolColor2, () => !Track.Playing, () =>
            {
                var swatch = CurrentTools.SelectedTool.Swatch;
                if (swatch != null)
                {
                    swatch.Selected = LineType.Red;
                }
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorToolColor3, () => !Track.Playing, () =>
            {
                var swatch = CurrentTools.SelectedTool.Swatch;
                if (swatch != null)
                {
                    swatch.Selected = LineType.Scenery;
                }
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorFocusFlag, () => !Track.Playing, () =>
            {
                var flag = Track.GetFlag();
                if (flag != null)
                {
                    Track.Camera.SetFrameCenter(flag.State.CalculateCenter());
                    Invalidate();
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorFocusRider, () => !Track.Playing, () =>
            {
                Track.Camera.SetFrameCenter(Track.RenderRider.CalculateCenter());
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorCancelTool,
            () => CurrentTools.SelectedTool.Active,
            () =>
            {
                var tool = CurrentTools.SelectedTool;
                var selecttool = CurrentTools.SelectTool;
                if (tool == selecttool)
                {
                    selecttool.CancelSelection();
                }
                else
                {
                    tool.Cancel();
                }
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.ToolCopy, () => !Track.Playing &&
            CurrentTools.SelectedTool == CurrentTools.SelectTool, () =>
            {
                CurrentTools.SelectTool.Copy();
                Invalidate();
            },
            null,
            repeat: false);
            InputUtils.RegisterHotkey(Hotkey.ToolCut, () => !Track.Playing &&
            CurrentTools.SelectedTool == CurrentTools.SelectTool, () =>
            {
                CurrentTools.SelectTool.Cut();
                Invalidate();
            },
            null,
            repeat: false);
            InputUtils.RegisterHotkey(Hotkey.ToolPaste, () => !Track.Playing &&
            (CurrentTools.SelectedTool == CurrentTools.SelectTool ||
            CurrentTools.SelectedTool == CurrentTools.MoveTool), () =>
            {
                CurrentTools.SelectTool.Paste();
                Invalidate();
            },
            null,
            repeat: false);
            InputUtils.RegisterHotkey(Hotkey.ToolDelete, () => !Track.Playing &&
            CurrentTools.SelectedTool == CurrentTools.SelectTool, () =>
            {
                CurrentTools.SelectTool.Delete();
                Invalidate();
            },
            null,
            repeat: false);
        }
        public void setScarfColor(int index, int color, byte opacity)
        {
            Track._renderer._riderrenderer.scarfColors[index] = color;
            Track._renderer._riderrenderer.scarfOpacity[index] = opacity;
        }
        public void addScarfColor(int color, byte opacity)
        {
            Track._renderer._riderrenderer.scarfColors.Add(color);
            Track._renderer._riderrenderer.scarfOpacity.Add(opacity);
        }
        public void insertScarfColor(int color, byte opacity, int index)
        {
            Track._renderer._riderrenderer.scarfColors.Insert(index, color);
            Track._renderer._riderrenderer.scarfOpacity.Insert(index, opacity);
        }
        public void removeScarfColor(int index)
        {
            Track._renderer._riderrenderer.scarfColors.RemoveAt(index);
            Track._renderer._riderrenderer.scarfOpacity.RemoveAt(index);
        }
        public List<int> getScarfColorList()
        {
            return Track._renderer._riderrenderer.scarfColors;
        }
        public List<byte> getScarfOpacityList()
        {
            return Track._renderer._riderrenderer.scarfOpacity;
        }
        public void removeAllScarfColors()
        {
            Track._renderer._riderrenderer.scarfColors.Clear();
            Track._renderer._riderrenderer.scarfOpacity.Clear();
        }
        public void shiftScarfColors(int shift) //Shifts scarf colors to the left
        {
            for (int i = 0; i < shift; i++)
            {
                insertScarfColor(getScarfColorList()[getScarfColorList().Count - 1], getScarfOpacityList()[getScarfOpacityList().Count - 1], 0);
                removeScarfColor(getScarfColorList().Count - 1);
            }
        }
    }
}
