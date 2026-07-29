using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using HotbarSlotType = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.HotbarSlotType;

namespace DozeAnywhere;

public sealed class Plugin : IDalamudPlugin
{
	private const int HorbarSize = 12;
	private const int HotbarsNum = 18;

	[PluginService]
	public static IClientState ClientState { get; private set; } = null!;

	[PluginService]
	public static ICommandManager CommandManager { get; private set; } = null!;

	[PluginService]
	public static IChatGui ChatGui { get; private set; } = null!;

	[PluginService]
	public static IKeyState KeyState { get; private set; } = null!;

	private unsafe RaptureHotbarModule* RaptureHotbarModule => Framework.Instance()
		->UIModule
		->GetRaptureHotbarModule();

	public Plugin()
	{
		CommandManager.AddHandler(
			"/xsit",
			GetExecuteEmotionCommand(
				50,
				96,
				"使用預設的 /sit 情感動作；按住 ALT 時可在任何地方坐下。"));

		CommandManager.AddHandler(
			"/xdoze",
			GetExecuteEmotionCommand(
				13,
				99,
				"使用預設的 /doze 情感動作；按住 ALT 時可在任何地方打瞌睡。"));

		CommandManager.AddHandler(
			"/createicons",
			new CommandInfo((_, args) => CreateRegularIcons(args.Split(" ")))
			{
				HelpMessage = "在指定快捷列建立可獨立使用的情感動作按鈕。不帶參數使用此命令可查看說明。",
			});
	}

	private unsafe void CreateRegularIcons(string[] args)
	{
		if (args.Length != 3)
		{
			ChatGui.PrintError("用法：");
			ChatGui.PrintError("/createicons sit 1 5 —— 在快捷列 1 的第 5 格建立「隨處坐下」動作。");
			ChatGui.PrintError("/createicons doze 1 5 —— 在快捷列 1 的第 5 格建立「隨處打瞌睡」動作。");
			return;
		}

		uint commandId;
		uint effectiveHotbarNumber;
		uint effectiveSlotPosition;

		switch (args[0])
		{
			case "sit":
				commandId = 96;
				break;
			case "doze":
				commandId = 99;
				break;
			default:
				ChatGui.PrintError("第一個參數必須是 sit 或 doze！");
				return;
		}

		if (!uint.TryParse(args[1], out var hotbarNumber) || hotbarNumber < 1 || hotbarNumber > HotbarsNum)
		{
			ChatGui.PrintError($"第二個參數必須是 1 至 {HotbarsNum} 之間的快捷列編號！");
			return;
		}
		else
		{
			effectiveHotbarNumber = hotbarNumber - 1;
		}

		
		if (!uint.TryParse(args[2], out var slotPosition) || slotPosition < 1 || slotPosition > HorbarSize)
		{
			ChatGui.PrintError($"第三個參數必須是 1 至 {HorbarSize} 之間的格位編號！");
			return;
		}
		else
		{
			effectiveSlotPosition = slotPosition - 1;
		}

		var hotbarSlot = RaptureHotbarModule->GetSlotById(
			effectiveHotbarNumber,
			effectiveSlotPosition);

		hotbarSlot->Set(HotbarSlotType.Emote, commandId);

		RaptureHotbarModule->WriteSavedSlot(
			RaptureHotbarModule->ActiveHotbarClassJobId,
			effectiveHotbarNumber,
			effectiveSlotPosition,
			hotbarSlot,
			false,
			ClientState.IsPvP);
	}

	private unsafe CommandInfo GetExecuteEmotionCommand(uint baseCommandId, uint innerCommandId, string commandHelpMessage)
	{
		return new CommandInfo((command, args) =>
		{
			if (ClientState.LocalPlayer is null || ClientState.LocalPlayer.IsDead)
			{
				return;
			}

			var effectiveCommandId = KeyState[VirtualKey.MENU]
				? innerCommandId
				: baseCommandId;

			RaptureHotbarModule->ScratchSlot.Set(HotbarSlotType.Emote, effectiveCommandId);
			RaptureHotbarModule->ExecuteSlot(&RaptureHotbarModule->ScratchSlot);
			RaptureHotbarModule->ScratchSlot.Set(HotbarSlotType.Empty, 0);
		})
		{
			HelpMessage = commandHelpMessage,
		};
	}

	public void Dispose()
	{
	}
}
