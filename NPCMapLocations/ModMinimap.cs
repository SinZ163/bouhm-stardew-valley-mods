using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations
{
	internal class ModMinimap
	{
		private readonly Texture2D BuildingMarkers;
		private readonly ModConfig Config;
		private readonly int borderWidth = 12;
		private Vector2 center;
		private float cropX;
		private float cropY;
		private readonly bool drawPamHouseUpgrade;
		private readonly Dictionary<long, MapMarker> FarmerMarkers;
		private readonly IModHelper Helper;

		public bool isBeingDragged;
		private readonly Texture2D map;
	  private int mmWidth; // minimap width
    private int mmHeight; // minimap height
	  private Vector2 mmPos; // top-left position of minimap relative to viewport
    private int mmX; // top-left crop of minimap relative to map
		private int mmY; // top-left crop of minimap relative to map
		private readonly HashSet<MapMarker> NpcMarkers;
		private Vector2 playerLoc;
		private int prevMmX;
		private int prevMmY;
		private int prevMouseX;
		private int prevMouseY;
	  private int drawDelay = 0;

		public ModMinimap(
			HashSet<MapMarker> npcMarkers,
			Dictionary<string, bool> secondaryNpcs,
			Dictionary<long, MapMarker> farmerMarkers,
			Dictionary<string, int> MarkerCropOffsets,
			Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings,
			Texture2D buildingMarkers,
			IModHelper helper,
			ModConfig config
		)
		{
			NpcMarkers = npcMarkers;
			SecondaryNpcs = secondaryNpcs;
			FarmerMarkers = farmerMarkers;
			this.MarkerCropOffsets = MarkerCropOffsets;
			FarmBuildings = farmBuildings;
			BuildingMarkers = buildingMarkers;
			Helper = helper;
			Config = config;

			map = Game1.content.Load<Texture2D>("LooseSprites\\map");
			drawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");

			mmX = Config.MinimapX;
			mmY = Config.MinimapY;
			mmWidth = Config.MinimapWidth * Game1.pixelZoom;
			mmHeight = Config.MinimapHeight * Game1.pixelZoom;
		}

		private Dictionary<string, bool> SecondaryNpcs { get; }
		private Dictionary<string, int> MarkerCropOffsets { get; }
		private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }

		public void HandleMouseDown()
		{
		  if (Game1.getMouseX() > mmX - borderWidth && Game1.getMouseX() < mmX + mmWidth + borderWidth &&
		      Game1.getMouseY() > mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth)
		  {
		    isBeingDragged = true;
		    prevMmX = mmX;
		    prevMmY = mmY;
		    prevMouseX = Game1.getMouseX();
		    prevMouseY = Game1.getMouseY();
		  }
		}

		public void HandleMouseRelease()
		{
		  if (Game1.getMouseX() > mmX - borderWidth && Game1.getMouseX() < mmX + mmWidth + borderWidth &&
		      Game1.getMouseY() > mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth)
		  {
		    isBeingDragged = false;
		    Config.MinimapX = mmX;
		    Config.MinimapY = mmY;
		    Helper.WriteConfig(Config);
		    drawDelay = 20;
		  }
		}

		public void Resize()
		{
			mmWidth = Config.MinimapWidth * Game1.pixelZoom;
			mmHeight = Config.MinimapHeight * Game1.pixelZoom;
		}

    public void Update()
		{
      center = ModMain.LocationToMap(Game1.player.currentLocation.Name, Game1.player.getTileX(),
				Game1.player.getTileY());
			playerLoc = center;

			center.X = NormalizeToMap(center.X);
			center.Y = NormalizeToMap(center.Y);

			// Top-left offset for markers, relative to the minimap
			mmPos =
				new Vector2(mmX - center.X + (float) Math.Floor(mmWidth / 2.0),
					mmY - center.Y + (float) Math.Floor(mmHeight / 2.0));

			// Top-left corner of minimap cropped from the whole map
			// Centered around the player's location on the map
			cropX = center.X - (float) Math.Floor(mmWidth / 2.0);
			cropY = center.Y - (float) Math.Floor(mmHeight / 2.0);

			// Handle cases when reaching edge of map 
			// Change offsets accordingly when player is no longer centered
			if (cropX < 0)
			{
				center.X = mmWidth / 2;
				mmPos.X = mmX;
				cropX = 0;
			}
			else if (cropX + mmWidth > map.Width * Game1.pixelZoom)
			{
				center.X = map.Width * Game1.pixelZoom - mmWidth / 2;
				mmPos.X = mmX - (map.Width * Game1.pixelZoom - mmWidth);

				cropX = map.Width - mmWidth;
			}

			if (cropY < 0)
			{
				center.Y = mmHeight / 2;
				mmPos.Y = mmY;
				cropY = 0;
			}
			// Actual map is 1200x720 but map.Height includes the farms
			else if (cropY + mmHeight > 720)
			{
				center.Y = 720 - mmHeight / 2;
				mmPos.Y = mmY - (720 - mmHeight);
				cropY = 720 - mmHeight;
			}
		}

		// Center or the player's position is used as reference; player is not center when reaching edge of map
		public void DrawMiniMap()
		{
		  var IsHoveringMinimap = false;

      if (Game1.getMouseX() > mmX - borderWidth && Game1.getMouseX() < mmX + mmWidth + borderWidth &&
			    Game1.getMouseY() > mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth)
			{
        IsHoveringMinimap = true;

        if (ModMain.HeldKey.ToString().Equals(Config.MinimapDragKey))
			    Game1.mouseCursor = 2;

        if (isBeingDragged)
				{
					mmX = (int) MathHelper.Clamp(prevMmX + Game1.getMouseX() - prevMouseX, 0 + borderWidth,
						Game1.viewport.Width - mmWidth - borderWidth);
					mmY = (int) MathHelper.Clamp(prevMmY + Game1.getMouseY() - prevMouseY, 0 + borderWidth,
						Game1.viewport.Height - mmHeight - borderWidth);
				}
			}

		  // When cursor is hovering over a clickable component behind the minimap, make transparent
			var b = Game1.spriteBatch;
			var color = IsHoveringMinimap
				? Color.White * 0.5f
				: Color.White;

			b.Draw(map, new Vector2(mmX, mmY),
				new Rectangle((int) Math.Floor(cropX / Game1.pixelZoom),
					(int) Math.Floor(cropY / Game1.pixelZoom), mmWidth / Game1.pixelZoom + 2,
					mmHeight / Game1.pixelZoom + 2), color, 0f, Vector2.Zero,
				4f, SpriteEffects.None, 0.86f);

		  if (!isBeingDragged)
		  {
		    // When minimap is moved, redraw markers after recalculating & repositioning
		    if (drawDelay == 0)
		    {
		      if (!IsHoveringMinimap)
		        DrawMarkers(b);
		    }
		    else
		      drawDelay--;
		  }

		  // Border around minimap that will also help mask markers outside of the minimap
			// Which gives more padding for when they are considered within the minimap area
			// Draw border
			DrawLine(b, new Vector2(mmX, mmY - borderWidth), new Vector2(mmX + mmWidth - 2, mmY - borderWidth), borderWidth,
				Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth), color*1.5f);
			DrawLine(b, new Vector2(mmX + mmWidth + borderWidth, mmY),
				new Vector2(mmX + mmWidth + borderWidth, mmY + mmHeight - 2), borderWidth, Game1.menuTexture,
				new Rectangle(8, 256, 3, borderWidth), color * 1.5f);
			DrawLine(b, new Vector2(mmX + mmWidth, mmY + mmHeight + borderWidth),
				new Vector2(mmX + 2, mmY + mmHeight + borderWidth), borderWidth, Game1.menuTexture,
				new Rectangle(8, 256, 3, borderWidth), color * 1.5f);
			DrawLine(b, new Vector2(mmX - borderWidth, mmHeight + mmY), new Vector2(mmX - borderWidth, mmY + 2), borderWidth,
				Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth), color * 1.5f);

			// Draw the border corners
			b.Draw(Game1.menuTexture, new Rectangle(mmX - borderWidth, mmY - borderWidth, borderWidth, borderWidth),
				new Rectangle(0, 256, borderWidth, borderWidth), color * 1.5f);
			b.Draw(Game1.menuTexture, new Rectangle(mmX + mmWidth, mmY - borderWidth, borderWidth, borderWidth),
				new Rectangle(48, 256, borderWidth, borderWidth), color * 1.5f);
			b.Draw(Game1.menuTexture, new Rectangle(mmX + mmWidth, mmY + mmHeight, borderWidth, borderWidth),
				new Rectangle(48, 304, borderWidth, borderWidth), color * 1.5f);
			b.Draw(Game1.menuTexture, new Rectangle(mmX - borderWidth, mmY + mmHeight, borderWidth, borderWidth),
				new Rectangle(0, 304, borderWidth, borderWidth), color * 1.5f);
		}

		private void DrawMarkers(SpriteBatch b)
		{
			var color = Color.White;

			// Farm overlay
			var farmCropWidth = (int) MathHelper.Min(131, (mmWidth - mmPos.X + Game1.tileSize / 4) / Game1.pixelZoom);
			var farmCropHeight = (int) MathHelper.Min(61, (mmHeight - mmPos.Y - 172 + Game1.tileSize / 4) / Game1.pixelZoom);
			switch (Game1.whichFarm)
			{
				case 1:
					b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 174)),
						new Rectangle(0, 180, farmCropWidth, farmCropHeight), color,
						0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
				case 2:
					b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 174)),
						new Rectangle(131, 180, farmCropWidth, farmCropHeight), color,
						0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
				case 3:
					b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 174)),
						new Rectangle(0, 241, farmCropWidth, farmCropHeight), color,
						0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
				case 4:
					b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 174)),
						new Rectangle(131, 241, farmCropWidth, farmCropHeight), color,
						0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
			}

			if (drawPamHouseUpgrade)
			{
				var pamHouseX = ModConstants.MapVectors["Trailer"][0].X;
				var pamHouseY = ModConstants.MapVectors["Trailer"][0].Y;
				if (IsWithinMapArea(pamHouseX, pamHouseY))
					b.Draw(map, new Vector2(NormalizeToMap(mmPos.X + pamHouseX), NormalizeToMap(mmPos.Y + pamHouseY)),
						new Rectangle(263, 181, 8, 8), color,
						0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
			}

			if (Config.ShowFarmBuildings && FarmBuildings != null)
			{
				var sortedBuildings = FarmBuildings.ToList();
				sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

				foreach (var building in sortedBuildings)
					if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out var buildingRect))
						if (IsWithinMapArea(building.Value.Value.X - buildingRect.Width / 2,
							building.Value.Value.Y - buildingRect.Height / 2))
							b.Draw(
								BuildingMarkers,
								new Vector2(
									NormalizeToMap(mmPos.X + building.Value.Value.X - (float) Math.Floor(buildingRect.Width / 2.0)),
									NormalizeToMap(mmPos.Y + building.Value.Value.Y - (float) Math.Floor(buildingRect.Height / 2.0))
								),
								buildingRect, color, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
							);
			}

			// Traveling Merchant
			if (Config.ShowTravelingMerchant && SecondaryNpcs["Merchant"])
			{
				var merchantLoc = ModMain.LocationToMap("Forest", 28, 11);
				if (IsWithinMapArea(merchantLoc.X - 16, merchantLoc.Y - 16))
					b.Draw(Game1.mouseCursors, new Vector2(NormalizeToMap(mmPos.X + merchantLoc.X - 16), NormalizeToMap(mmPos.Y + merchantLoc.Y - 15)), new Rectangle(191, 1410, 22, 21), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
						1f);
			}

				if (Context.IsMultiplayer)
				{
					foreach (var farmer in Game1.getOnlineFarmers())
					{
						// Temporary solution to handle desync of farmhand location/tile position when changing location
						if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
						{
							if (farMarker.DrawDelay == 0 &&
							    IsWithinMapArea(farMarker.MapLocation.X - 16, farMarker.MapLocation.Y - 15))
							{
								farmer.FarmerRenderer.drawMiniPortrat(b,
									new Vector2(NormalizeToMap(mmPos.X + farMarker.MapLocation.X - 16),
										NormalizeToMap(mmPos.Y + farMarker.MapLocation.Y - 15)),
									0.00011f, 2f, 1, farmer);
							}
						}
					}
				}
				else
				{
					Game1.player.FarmerRenderer.drawMiniPortrat(b,
						new Vector2(NormalizeToMap(mmPos.X + playerLoc.X - 16), NormalizeToMap(mmPos.Y + playerLoc.Y - 15)), 0.00011f,
						2f, 1,
						Game1.player);
				}
			

			// NPCs
			// Sort by drawing order
			var sortedMarkers = NpcMarkers.ToList();
			sortedMarkers.Sort((x, y) => x.Layer.CompareTo(y.Layer));

			foreach (var npcMarker in sortedMarkers)
			{
				// Skip if no specified location
				if (npcMarker.MapLocation == Vector2.Zero || npcMarker.Marker == null ||
				    !MarkerCropOffsets.ContainsKey(npcMarker.Npc.Name) ||
				    !IsWithinMapArea(npcMarker.MapLocation.X, npcMarker.MapLocation.Y))
					continue;

				// Tint/dim hidden markers
				if (npcMarker.IsHidden)
				{
					b.Draw(npcMarker.Marker,
						new Rectangle(NormalizeToMap(mmPos.X + npcMarker.MapLocation.X),
							NormalizeToMap(mmPos.Y + npcMarker.MapLocation.Y),
							32, 30),
						new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15), Color.DimGray * 0.7f);
					if (npcMarker.IsBirthday)
						b.Draw(Game1.mouseCursors,
							new Vector2(NormalizeToMap(mmPos.X + npcMarker.MapLocation.X + 20),
								NormalizeToMap(mmPos.Y + npcMarker.MapLocation.Y)),
							new Rectangle(147, 412, 10, 11), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
							SpriteEffects.None, 0f);

					if (npcMarker.HasQuest)
						b.Draw(Game1.mouseCursors,
							new Vector2(NormalizeToMap(mmPos.X + npcMarker.MapLocation.X + 22),
								NormalizeToMap(mmPos.Y + npcMarker.MapLocation.Y - 3)),
							new Rectangle(403, 496, 5, 14), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
							SpriteEffects.None, 0f);
				}
				else
				{
					b.Draw(npcMarker.Marker,
						new Rectangle(NormalizeToMap(mmPos.X + npcMarker.MapLocation.X),
							NormalizeToMap(mmPos.Y + npcMarker.MapLocation.Y),
							30, 32),
						new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15), Color.White);
					if (npcMarker.IsBirthday)
						b.Draw(Game1.mouseCursors,
							new Vector2(NormalizeToMap(mmPos.X + npcMarker.MapLocation.X + 20),
								NormalizeToMap(mmPos.Y + npcMarker.MapLocation.Y)),
							new Rectangle(147, 412, 10, 11), Color.White, 0f, Vector2.Zero, 1.8f,
							SpriteEffects.None,
							0f);

					if (npcMarker.HasQuest)
						b.Draw(Game1.mouseCursors,
							new Vector2(NormalizeToMap(mmPos.X + npcMarker.MapLocation.X + 22),
								NormalizeToMap(mmPos.Y + npcMarker.MapLocation.Y - 3)),
							new Rectangle(403, 496, 5, 14), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
							0f);
				}
			}
		}

		// Normalize offset differences caused by map being 4x less precise than map markers 
		// Makes the map and markers move together instead of markers moving more precisely (moving when minimap does not shift)
		private int NormalizeToMap(float n)
		{
			return (int) Math.Floor(n / Game1.pixelZoom) * Game1.pixelZoom;
		}

		// Check if within map
		private bool IsWithinMapArea(float x, float y)
		{
			return x > center.X - mmWidth / 2 - (Game1.tileSize / 4 + 2)
			       && x < center.X + mmWidth / 2 - (Game1.tileSize / 4 + 2)
			       && y > center.Y - mmHeight / 2 - (Game1.tileSize / 4 + 2)
			       && y < center.Y + mmHeight / 2 - (Game1.tileSize / 4 + 2);
		}

		// For borders
		private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, int width, Texture2D tex, Rectangle srcRect, Color color)
		{
			var r = new Rectangle((int) begin.X, (int) begin.Y, (int) (end - begin).Length() + 2, width);
			var v = Vector2.Normalize(begin - end);
			var angle = (float) Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
			if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
			b.Draw(tex, r, srcRect, color, angle, Vector2.Zero, SpriteEffects.None, 0);
		}
	}
}