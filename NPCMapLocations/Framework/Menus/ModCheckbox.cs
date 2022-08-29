﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    // Mod checkbox for settings and npc blacklist
    internal class ModCheckbox : OptionsElement
    {
        /*********
        ** Fields
        *********/
        private readonly KeyValuePair<string, NpcMarker>? NpcMarker;
        private bool IsChecked;


        /*********
        ** Public methods
        *********/
        public ModCheckbox(string label, int whichOption, KeyValuePair<string, NpcMarker>? npcMarker = null)
            : base(label, -1, -1, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.NpcMarker = npcMarker;

            // Villager names
            if (whichOption > 12 && npcMarker != null)
                this.IsChecked = !ModEntry.ShouldExcludeNpc(npcMarker.Value.Key);
            else
            {
                this.IsChecked = whichOption switch
                {
                    0 => ModEntry.Globals.ShowMinimap,
                    5 => ModEntry.Globals.LockMinimapPosition,
                    6 => ModEntry.Globals.OnlySameLocation,
                    7 => ModEntry.Config.ByHeartLevel,
                    10 => ModEntry.Globals.ShowQuests,
                    11 => ModEntry.Globals.ShowHiddenVillagers,
                    12 => ModEntry.Globals.ShowTravelingMerchant,
                    _ => false
                };
            }
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.receiveLeftClick(x, y);
            this.IsChecked = !this.IsChecked;
            int whichOption = this.whichOption;

            // Show/hide villager options
            if (whichOption > 12 && this.NpcMarker != null)
            {
                string name = this.NpcMarker.Value.Key;
                bool exclude = !this.IsChecked;

                if (exclude == ModEntry.ShouldExcludeNpc(name, ignoreConfig: true))
                    ModEntry.Config.ForceNpcVisibility.Remove(name);
                else
                    ModEntry.Config.ForceNpcVisibility[name] = exclude;
            }
            else
            {
                switch (whichOption)
                {
                    case 0:
                        ModEntry.Globals.ShowMinimap = this.IsChecked;
                        break;
                    case 5:
                        ModEntry.Globals.LockMinimapPosition = this.IsChecked;
                        break;
                    case 6:
                        ModEntry.Globals.OnlySameLocation = this.IsChecked;
                        break;
                    case 7:
                        ModEntry.Config.ByHeartLevel = this.IsChecked;
                        break;
                    case 10:
                        ModEntry.Globals.ShowQuests = this.IsChecked;
                        break;
                    case 11:
                        ModEntry.Globals.ShowHiddenVillagers = this.IsChecked;
                        break;
                    case 12:
                        ModEntry.Globals.ShowTravelingMerchant = this.IsChecked;
                        break;
                }
            }

            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
            ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", ModEntry.Globals);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y),
                this.IsChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked,
                Color.White * (this.greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None,
                0.4f);
            if (this.whichOption > 12 && this.NpcMarker != null)
            {
                var marker = this.NpcMarker.Value.Value;

                if (this.IsChecked)
                    Game1.spriteBatch.Draw(marker.Sprite, new Vector2((float)slotX + this.bounds.X + 50, slotY),
                        new Rectangle(0, marker.CropOffset, 16, 15), Color.White, 0f, Vector2.Zero,
                        Game1.pixelZoom, SpriteEffects.None, 0.4f);
                else
                    Game1.spriteBatch.Draw(marker.Sprite, new Vector2((float)slotX + this.bounds.X + 50, slotY),
                        new Rectangle(0, marker.CropOffset, 16, 15), Color.White * 0.33f, 0f, Vector2.Zero,
                        Game1.pixelZoom, SpriteEffects.None, 0.4f);

                // Draw names
                slotX += 75;
                if (this.whichOption == -1)
                    SpriteText.drawString(b, this.label, slotX + this.bounds.X, slotY + this.bounds.Y + 12, 999, -1, 999, 1f, 0.1f);
                else
                    Utility.drawTextWithShadow(b, marker.DisplayName, Game1.dialogueFont,
                        new Vector2(slotX + this.bounds.X + this.bounds.Width + 8, slotY + this.bounds.Y),
                        this.greyedOut ? Game1.textColor * 0.33f : Game1.textColor, 1f, 0.1f);
            }
            else
            {
                base.draw(b, slotX, slotY, context);
            }
        }
    }
}
