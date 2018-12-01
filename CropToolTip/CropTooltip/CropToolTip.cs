using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Bookcase.Events;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using System.Collections;

namespace CropToolTip
{
    public class ModEntry : Mod
    {
        private ArrayList crop_name = new ArrayList(0);
        private bool mouseHoverShow;
        private bool showTimeTillHarvest;
        private SButton toggleKeybind;
        private bool showIfDies;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            ConfigOptions config = helper.ReadConfig<ConfigOptions>();

            mouseHoverShow = config.Show_On_Mouse_Hover_Default;
            showTimeTillHarvest = config.Show_Harvest_Time_Left;
            toggleKeybind = config.Toggle_Keybind;
            showIfDies = config.Show_If_Plant_Will_Die_Before_Harvest;

            InputEvents.ButtonPressed += this.InputEvents_ButtonPressed;
            BookcaseEvents.GameQuaterSecondTick.Add(QuaterSecondUpdate);
            GraphicsEvents.OnPostRenderHudEvent += new EventHandler(this.GraphicsEvents_OnPostRenderHudEvent);
        }

        /// <summary>
        /// Mod updates every 250ms for performance.
        /// </summary>
        /// <param name="e"></param>
        private void QuaterSecondUpdate(Bookcase.Events.Event e)
        {
            // If world is not ready or mouseHover is turned off. 
            if (!Context.IsWorldReady || !mouseHoverShow)
                return;
            CheckForTile();
        }

        private void GraphicsEvents_OnPostRenderHudEvent(object sender, EventArgs e)
        {
            if (!Context.CanPlayerMove || this.crop_name == null || this.crop_name.Count == 0 || !mouseHoverShow)
                return;

            int num1 = 64;
            SpriteFont smallFont = Game1.smallFont;
            SpriteBatch spriteBatch = Game1.spriteBatch;

            Vector2 vector2 = smallFont.MeasureString("");
            foreach (String str in this.crop_name)
            {
                Vector2 temp = smallFont.MeasureString(str);
                if (temp.X > vector2.X)
                    vector2 = temp;
            }

            int num2 = num1 / 2;
            int width = (int)((double)vector2.X + (double)num2);
            int height = Math.Max(60, 60 + 35 * (this.crop_name.Count - 1));
            int x = Game1.getOldMouseX() + num2;
            int y = Game1.getOldMouseY() + num2;
            if (x + width > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - width;
                y += num2;
            }
            if (y + height > Game1.viewport.Height)
            {
                x += num2;
                y = Game1.viewport.Height - height;
            }
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
            int cnt = 0;
            foreach (String str in this.crop_name)
            {
                Utility.drawTextWithShadow(spriteBatch, str, smallFont, new Vector2((float)(x + num1 / 4), (float)(y + num1 / 4) + (cnt * vector2.Y)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                cnt++;
            }
        }


        private void CheckForTile()
        {
            if (Game1.gameMode == Game1.playingGameMode && Game1.player != null && Game1.player.currentLocation != null)
            {
                this.crop_name.Clear();
                Vector2 Tile = Game1.currentCursorTile;
                IEnumerable<KeyValuePair<Vector2, TerrainFeature>> features = Game1.player.currentLocation.terrainFeatures.Pairs;
                StardewValley.Object objectAtTile = Game1.player.currentLocation.getObjectAtTile((int)Game1.currentCursorTile.X, (int)Game1.currentCursorTile.Y);

                if (Game1.player.currentLocation.isCropAtTile((int)Tile.X, (int)Tile.Y))
                {
                    if (Game1.player.currentLocation.terrainFeatures.ContainsKey(Tile))
                    {
                        StardewValley.Crop cropAtTile = ((HoeDirt)Game1.player.currentLocation.terrainFeatures[Tile]).crop;
                        bool needsWatering = ((HoeDirt)Game1.player.currentLocation.terrainFeatures[Tile]).needsWatering();
                        string name;
                        Game1.objectInformation.TryGetValue(cropAtTile.indexOfHarvest.Value, out name);
                        name = name.Split('/')[4];
                        this.crop_name.Add(name);

                        int maxStages = cropAtTile.phaseDays.Count - 1;
                        int currentStage = Math.Min(cropAtTile.phaseDays.Count - 1, cropAtTile.currentPhase.Value);
                        int currentphaseValue = Math.Max(cropAtTile.dayOfCurrentPhase.Value, 0);
                        int maxdays = 0;
                        int daysleft = 0;

                        for (int i = 0; i < cropAtTile.phaseDays.Count - 1; i++)
                        {
                            maxdays += cropAtTile.phaseDays[i];
                            if (i < currentStage && currentStage > 0)
                                daysleft += cropAtTile.phaseDays[i];
                        }
                        daysleft += currentphaseValue;
                        daysleft = maxdays - daysleft;

                        int endofMonth = 28 - Game1.dayOfMonth;

                        if (cropAtTile.fullyGrown.Value && daysleft < 0)
                            daysleft = Math.Abs(daysleft);
                        else if (daysleft < 0)
                            daysleft = 0;
                        
                        this.Monitor.Log($"End of the Month: {endofMonth} ... {daysleft} ... can harvest: { (daysleft <= endofMonth)} ");
                        if (cropAtTile.dead.Value)
                            this.crop_name.Add("Is dead!");
                        else if (daysleft == 0)
                            this.crop_name.Add("Ready to harvest!");
                        else
                            this.crop_name.Add($"Harvest in {daysleft} {(daysleft > 1 ? "days" : "day")}");
                    }
                }
                else
                {
                    this.crop_name.Clear();
                }

            }
        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method invoked when the player presses a controller, keyboard, or mouse button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button.Equals(toggleKeybind))
            {
                mouseHoverShow = !mouseHoverShow;

                Game1.addHUDMessage(new HUDMessage($"{(mouseHoverShow ? "Show" : "Hide")} Crop ToolTip", null));
            }

        }
    }
}
