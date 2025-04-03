using GeonBit.UI.Entities;
using GeonBit.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace FantasyVoxels.UI
{
    public static class UIUtils
    {
        public static string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = spriteFont.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = spriteFont.MeasureString(word);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }

            return sb.ToString();
        }
        public static void DrawItem(Vector2 pos, float uiScale, int id, int stack, Item bitem, float depth = 0.5f, bool blackout = false)
        {
            const float itemSize = MGame.ItemAtlasSize / 16f, blockSize = MGame.AtlasSize / 16f;
            //Compound item
            if (id == -2)
            {
                if (bitem.properties is ToolProperties)
                {
                    var item = ItemManager.GetItemFromID(((ToolProperties)bitem.properties).toolHandle);
                    if (item.type == ItemType.Block)
                    {
                        int tex = Voxel.voxelTypes[item.placement].frontTexture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((int)(tex % blockSize) * 16, (int)(tex / blockSize) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth);
                    }
                    else
                    {
                        int tex = item.texture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.items,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((int)(tex % itemSize) * 16, (int)(tex / itemSize) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth);
                    }
                    item = ItemManager.GetItemFromID(((ToolProperties)bitem.properties).toolHead);
                    if (item.type == ItemType.Block)
                    {
                        int tex = Voxel.voxelTypes[item.placement].frontTexture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((int)(tex % blockSize) * 16, (int)(tex / blockSize) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth + 0.001f);
                    }
                    else
                    {
                        int tex = item.texture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.items,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((int)(tex % itemSize) * 16, (int)(tex / itemSize) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth + 0.001f);
                    }
                }
            }
            else
            {
                var item = ItemManager.GetItemFromID(id);
                if (item.type == ItemType.Block)
                {
                    int tex = Voxel.voxelTypes[item.placement].frontTexture;

                    MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                    new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                    new Rectangle((int)(tex % blockSize) * 16, (int)(tex / blockSize) * 16, 16, 16), blackout? Color.Black : Color.White, 0, Vector2.Zero,
                                                    Vector2.One * uiScale,
                                                    SpriteEffects.None,
                                                    depth);
                }
                else
                {
                    int tex = item.texture;

                    MGame.Instance.spriteBatch.Draw(MGame.Instance.items,
                                                    new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                    new Rectangle((int)(tex % itemSize) * 16, (int)(tex / itemSize) * 16, 16, 16), blackout ? Color.Black : Color.White, 0, Vector2.Zero,
                                                    Vector2.One * uiScale,
                                                    SpriteEffects.None,
                                                    depth);
                }

                if (stack <= 1) return;

                Vector2 shift = Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(stack.ToString()) * uiScale;

                MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], stack.ToString(), new Vector2(pos.X + 21 * uiScale, pos.Y - 0 * uiScale) - shift, Color.Black, 0f, Vector2.Zero, Vector2.One * (uiScale), SpriteEffects.None, depth + 0.01f);
                MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], stack.ToString(), new Vector2(pos.X + 20 * uiScale, pos.Y - 1 * uiScale) - shift, Color.White, 0f, Vector2.Zero, Vector2.One * (uiScale), SpriteEffects.None, depth + 0.02f);
            }
        }
        public static void RenderTooltip(Item item, int scale)
        {
            if (item.itemID != -1)
            {
                string displayname = item.itemID >= 0 ? ItemManager.GetItemFromID(item.itemID).displayName :
                                                        ItemManager.GetItemFromID(((ToolProperties)item.properties).toolHead).displayName.TrimEnd("Tool-Head".ToCharArray()).Trim();

                StringBuilder extras = new StringBuilder();

                if (item.itemID == -2)
                {
                    if (item.properties is ToolProperties prop)
                    {
                        extras.AppendLine($"Made from: {ItemManager.GetItemFromID(prop.toolHead).displayName} + {ItemManager.GetItemFromID(prop.toolHandle).displayName}");
                        extras.AppendLine($"Breaks: {((ToolPieceProperties)ItemManager.GetItemFromID(prop.toolHead).properties).meantFor}");
                        extras.AppendLine($"Durability: {prop.durability}/{prop.maxDurability}");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(ItemManager.GetItemFromID(item.itemID).description)) extras.AppendLine(ItemManager.GetItemFromID(item.itemID).description);

                    if (ItemManager.GetItemFromID(item.itemID).properties is ToolPieceProperties prop)
                    {
                        if (prop.slot == ToolPieceSlot.Head)
                        {
                            extras.AppendLine($"Tool level: {prop.toolPieceLevel}");
                            extras.AppendLine($"Breaks: {prop.meantFor}");
                        }
                        extras.AppendLine($"Speculative Durability: {prop.durability}");
                    }
                }

                string extraInfo = UIUtils.WrapText(Resources.Instance.Fonts[(int)FontStyle.Regular], extras.ToString(), 200);

                Vector2 size = (Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(displayname) + Vector2.One * 2) * scale;

                if (!string.IsNullOrWhiteSpace(extraInfo)) size = Vector2.Max((Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(extraInfo) + new Vector2(2, 32)) * (scale / 2f), size);

                Vector2 pos = (Vector2.Floor((Mouse.GetState().Position.ToVector2()) / scale)) * scale + new Vector2(8, 8) * scale;

                MGame.Instance.spriteBatch.Draw(MGame.Instance.white, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X + scale * 5, (int)size.Y), null, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, 0.99f);
                MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], displayname, pos + Vector2.UnitX * scale * 4, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                if (!string.IsNullOrWhiteSpace(extraInfo)) MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], extraInfo, pos + Vector2.UnitX * scale * 4 + Vector2.UnitY * scale * 16, Color.Yellow, 0f, Vector2.Zero, scale / 2, SpriteEffects.None, 1f);
            }
		}
		public static void RenderTooltip(string tooltip, string details, int scale)
        {
            string displayname = tooltip;
			string extraInfo = details;

			Vector2 size = (Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(displayname) + Vector2.One * 2) * scale;

			if (!string.IsNullOrWhiteSpace(extraInfo)) size = Vector2.Max((Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(extraInfo) + new Vector2(2, 32)) * (scale / 2f), size);

			Vector2 pos = (Vector2.Floor((Mouse.GetState().Position.ToVector2()) / scale)) * scale + new Vector2(8, 8) * scale;

			MGame.Instance.spriteBatch.Draw(MGame.Instance.white, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X + scale * 5, (int)size.Y), null, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, 0.99f);
			MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], displayname, pos + Vector2.UnitX * scale * 4, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
			if (!string.IsNullOrWhiteSpace(extraInfo)) MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], extraInfo, pos + Vector2.UnitX * scale * 4 + Vector2.UnitY * scale * 16, Color.Yellow, 0f, Vector2.Zero, scale / 2, SpriteEffects.None, 1f);
		}
	}
}
