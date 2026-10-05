using Godot;
using Game.Temperance.Network;
using Game.Temperance.Signals;

namespace Game.Client.Scenes;

public partial class DebugDisplay : Control
{
	[Export] private RichTextLabel? _systemsDebugLabel;
	[Export] private ItemList? _systemsList;

	private readonly SignalBus _signalBus = SignalBus.Instance;
	
	private const string DebugOptionName = "FPS and Ping";
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_signalBus.UpdateDebugLabel += OnUpdateDebugLabel;
		
		_systemsList?.Clear();
		_systemsList?.AddItem(DebugOptionName);
		var signal = new FetchDebugMenuOptions()
		{
			SystemsList = _systemsList
		};
		_signalBus.EmitFetchDebugMenuOptions(ref signal);
		_systemsList?.SortItemsByText();
		_systemsList?.Select(0);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		base._UnhandledKeyInput(@event);

		if (@event is not InputEventAction inputEventAction || inputEventAction.Action != "show_debug")
			return;

		if (_systemsList is null)
			return;

		if (!_systemsList.IsVisible())
			_systemsList.Visible = true;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_systemsList is null)
			return;

		var selectedSystem = _systemsList.GetItemText(_systemsList.GetSelectedItems()[0]);
		var signal = new UpdateDebugLabel(selectedSystem)
		{
			DebugLabel = _systemsDebugLabel
		};
		_signalBus.EmitUpdateDebugLabel(selectedSystem, ref signal);
	}

	private void OnUpdateDebugLabel(string selectedSystem, ref UpdateDebugLabel args)
	{
		if (args.DebugLabel is null)
			return;

		if (selectedSystem != DebugOptionName)
			return;

		args.DebugLabel.Clear();
		args.DebugLabel.AppendText("FPS: " + Engine.GetFramesPerSecond().ToString("N0") + "\n");
		args.DebugLabel.AppendText("Ping: " + ENetClient.Instance.GetPing().ToString() + "\n");
		args.DebugLabel.AppendText("Packet Loss: " + ENetClient.Instance.GetPacketLoss().ToString("P") + "\n");
	}
}

public partial class FetchDebugMenuOptions : UserSignalArgs
{
	public ItemList? SystemsList;
}

public partial class UpdateDebugLabel : UserSignalArgs
{
	public string SelectedSystem;
	public RichTextLabel? DebugLabel;

	public UpdateDebugLabel(string selectedSystem)
	{
		SelectedSystem = selectedSystem;
	}
}