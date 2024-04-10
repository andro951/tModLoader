using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.ID;
using Terraria.ModLoader.Core;

namespace Terraria.ModLoader;
/// <summary>
/// This serves as the central class from which liquid-related functions are carried out.
/// </summary>
public static class LiquidLoader
{
	internal struct LiquidProperties
	{
		public int FallDelay;
		public bool NoStandardUpdate;
		public bool HellEvaporation;
	}

	private static int nextLiquid = LiquidID.Count;
	private static bool loaded = false;

	internal static readonly IList<ModLiquid> liquids = new List<ModLiquid>();

	internal static LiquidProperties[] liquidProperties;

	public static int LiquidCount => nextLiquid;

	public static ModLiquid GetLiquid(int type)
	{
		return type >= LiquidID.Count && type < LiquidCount ? liquids[type - LiquidID.Count] : null;
	}

	internal static int ReserveLiquidID()
	{
		if (ModNet.AllowVanillaClients) 
			throw new Exception("Adding liquid breaks vanilla client compatibility");
		int reserveId = nextLiquid;
		nextLiquid++;
		return reserveId;
	}

	internal static readonly IList<GlobalLiquid> globalLiquids = new List<GlobalLiquid>();
	internal static void ResizeArrays(bool unloading = false)
	{
		//Texture
		Array.Resize(ref TextureAssets.Liquid, 15 + nextLiquid - LiquidID.Count);

		//Sets
		LoaderUtils.ResetStaticMembers(typeof(LiquidID));

		//Etc
		Array.Resize(ref LiquidRenderer.WATERFALL_LENGTH, nextLiquid);
		Array.Resize(ref LiquidRenderer.DEFAULT_OPACITY, nextLiquid);
		Array.Resize(ref LiquidRenderer.WAVE_MASK_STRENGTH, nextLiquid + 1);
		Array.Resize(ref LiquidRenderer.VISCOSITY_MASK, nextLiquid + 1);
		Array.Resize(ref LiquidLoader.liquidProperties, nextLiquid);
		Array.Resize(ref Main.SceneMetrics._liquidCounts, nextLiquid);
		Array.Resize(ref Main.PylonSystem._sceneMetrics._liquidCounts, nextLiquid);

		//Hooks
		ModLoader.BuildGlobalHook(ref HookCanMoveLeft, globalLiquids, g => g.CanMoveLeft);
		ModLoader.BuildGlobalHook(ref HookCanMoveRight, globalLiquids, g => g.CanMoveRight);
		ModLoader.BuildGlobalHook(ref HookCanMoveDown, globalLiquids, g => g.CanMoveDown);
		ModLoader.BuildGlobalHook(ref HookShouldDrawLiquids, globalLiquids, g => g.ShouldDrawLiquids);

		if (!unloading) {
			loaded = true;
		}
	}

	internal static void Unload()
	{
		loaded = false;
		nextLiquid = LiquidID.Count;
		liquids.Clear();
	}

	public static void ModifyLight(int type, int i, int j, ref float r, ref float g, ref float b)
	{
		GetLiquid(type)?.ModifyLight(i, j, ref r, ref g, ref b);
	}

	public static void PreUpdate(int type, int x, int y)
	{
		GetLiquid(type)?.PreUpdate(x, y);
	}

	public static bool Update(int type, Liquid liquid, int x, int y, Tile left, Tile right, Tile up, Tile down)
	{
		return GetLiquid(type)?.Update(liquid, x, y, left, right, up, down) ?? true;
	}

	public static void Merge(int type, bool[] liquidNearby, ref int liquidMergeTileType, ref int liquidMergeType)
	{
		GetLiquid(type)?.Merge(type, liquidNearby, ref liquidMergeTileType, ref liquidMergeType);
	}

	static LiquidLoader()
	{
		liquidProperties = new LiquidProperties[] {
			// Water
			new LiquidProperties() {
				FallDelay = 0,
				HellEvaporation = true
			},
			// Lava
			new LiquidProperties() {
				FallDelay = 5
			},
			// Honey
			new LiquidProperties() {
				FallDelay = 10
			},
			//Shimmer
			new LiquidProperties() {
				FallDelay = 0
			}};
	}

	private delegate bool? DelegateCanMove(int x, int y, int xMove, int yMove, bool canMoveLeftVanilla);
	private static DelegateCanMove[] HookCanMoveLeft;
	public static bool CanMoveLeft(int x, int y, int xMove, int yMove, bool canMoveLeftVanilla)
	{
		bool? result = null;

		foreach (var hook in HookCanMoveLeft) {
			bool? move = hook(x, y, xMove, yMove, canMoveLeftVanilla);
			if (move.HasValue) {
				result = move;
				if (!move.Value)
					return false;
			}
		}

		return result ?? canMoveLeftVanilla;
	}

	private delegate bool? DelegateCanMoveRight(int x, int y, int xMove, int yMove, bool canMoveRightVanilla);
	private static DelegateCanMove[] HookCanMoveRight;
	public static bool? CanMoveRight(int x, int y, int xMove, int yMove, bool canMoveRightVanilla)
	{
		bool? result = null;

		foreach (var hook in HookCanMoveRight) {
			bool? move = hook(x, y, xMove, yMove, canMoveRightVanilla);
			if (move.HasValue) {
				result = move;
				if (!move.Value)
					return false;
			}
		}

		return result ?? canMoveRightVanilla;
	}

	private delegate bool? DelegateCanMoveDown(int x, int y, int xMove, int yMove, bool canMoveDownVanilla);
	private static DelegateCanMove[] HookCanMoveDown;
	public static bool CanMoveDown(int x, int y, int xMove, int yMove, bool canMoveDownVanilla)
	{
		bool? result = null;

		foreach (var hook in HookCanMoveDown) {
			bool? move = hook(x, y, xMove, yMove, canMoveDownVanilla);
			if (move.HasValue) {
				result = move;
				if (!move.Value)
					return false;
			}
		}

		return result ?? canMoveDownVanilla;
	}

	private delegate bool? DelegateShouldDrawLiquids(int x, int y, bool shouldDrawVanilla);
	private static DelegateShouldDrawLiquids[] HookShouldDrawLiquids;
	public static bool ShouldDrawLiquids(int x, int y, bool shouldDrawVanilla)
	{
		bool? result = null;

		foreach (var hook in HookShouldDrawLiquids) {
			bool? draw = hook(x, y, shouldDrawVanilla);
			if (draw.HasValue) {
				result = draw;
				if (!draw.Value)
					return false;
			}
		}

		return result ?? shouldDrawVanilla;
	}
}
